using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
// for sales force
using GED.Tools.WSDLQualif;



namespace GED.Handlers
{
    //SINGLETON !
    public class Production
    {
        private static Production refInstance;

        //method to get the instance of class
        public static Production getInstance(){
            Production refInstance =  null;
            try {
                if (refInstance == null)
                {
                    refInstance = new Production();
                }
                return refInstance;
            } catch(Exception ex)
            {
                // we mustn't be here !
                Console.WriteLine("exception throwed ==> {0}", ex.Message);
            }
            return refInstance;
        }

        private Production(){}

        //** method to send a List of 'Acte'
        public async Task<bool> envoyerProd(List<Acte> actes)
        {
            int nombreActes = actes.Count;
            Dictionary<string, WsResponse> cresponses = new Dictionary<string, WsResponse>();
            for (int i = 0; i < nombreActes; i++){
            // if i pass TRANSTYPE TABLE here as method parameter, The context will depend on the company (unless TRANSTYPE table concerne all companies)
                //Dynamic Dyspatching
                IActe acteprod = new Spirica(actes[i]);
                Dictionary<string, WsResponse> currentResponse = new Dictionary<string, WsResponse>();
                currentResponse = await acteprod.sendProd(); // send one "Acte" *** (dic with one element)
                cresponses.Add(currentResponse.Keys.ElementAt(0), currentResponse[currentResponse.Keys.ElementAt(0)]); // get current element???
            }
            updateSalesForce(cresponses);
            bool prodState = Spirica.getProdState();
            return prodState;
        }// must return boolean

        //method to update salesForce records all lsit is retrived (Not working for lack of permission), could be fixed if i had the SF acte id
        public void updateSalesForceV1(Dictionary<string,WsResponse> responses){
            //Build Query
            string[] idActes = responses.Keys.ToArray();//actes.Select(p => p.ReferenceInterne).ToArray(); // extract only keys (ids)
            string idList = "'" + String.Join("','", idActes) + "'";
            string soqlQuery = "SELECT Id, Name, Commentaire_Interne__c, Statut_du_XML__c FROM Acte__c WHERE Name in (" + idList + ")";
            // IDS
            string username = "noluser@nortia.fr.nqualif";//#
            string passwd = "nortia01";//#
            // call ws
            SforceService SfService = new GED.Tools.WSDLQualif.SforceService(); 
            //Execute query
            try
            {
                LoginResult loginResult = SfService.login(username, passwd);
                SfService.Url = loginResult.serverUrl;
                SfService.SessionHeaderValue = new SessionHeader();
                SfService.SessionHeaderValue.sessionId = loginResult.sessionId;

                QueryResult result = SfService.query(soqlQuery);
                Acte__c[] SfActes = new Acte__c[result.size];

                for (int i = 0; i < result.size; i++){
                    SfActes[i] = (Acte__c)result.records[i];
                    // update data
                    string retMessage = string.Join(" ", responses[SfActes[i].Name].message);
                    SfActes[i].Commentaire_Interne__c += retMessage;
                    SfActes[i].Statut_du_XML__c = responses[SfActes[i].Name].status_xml;
                }
                // save update
                SaveResult[] saveResults = SfService.update(SfActes); //==> "Unable to create/update fields: Name. Please check the security settings of this field and verify that it is read/write for your profile or permission set.";
            }
            catch (Exception ex){
                SfService = null;
                //throw (ex); // you shall not pass
            }
        }

        //update "acte" by "acte" this fucntion consume more time/space than updateSalesForce but fix permission issue
        public void updateSalesForce(Dictionary<string,WsResponse> responses){
            // IDS
            string username = "noluser@nortia.fr.nqualif";//#
            string passwd = "nortia01";//#
        
            SforceService SfService = new GED.Tools.WSDLQualif.SforceService();
            LoginResult loginResult = SfService.login(username, passwd);
            SfService.Url = loginResult.serverUrl;
            SfService.SessionHeaderValue = new SessionHeader();
            SfService.SessionHeaderValue.sessionId = loginResult.sessionId;

            foreach(KeyValuePair<string, WsResponse> response in responses){
                Acte__c SfActe = new Acte__c();
                string soqlQuery = "SELECT Id, Commentaire_Interne__c, Statut_du_XML__c FROM Acte__c WHERE Name = '" + response.Key + "'";
                QueryResult result = SfService.query(soqlQuery);
                if(result.size != 0)
                SfActe = (Acte__c)result.records[0]; // take the only item
                // update data
                SfActe.Commentaire_Interne__c = string.Join(" ", responses[response.Key].message);
                SfActe.Statut_du_XML__c = responses[response.Key].status_xml;
                SaveResult[] saveResults = SfService.update(new sObject[] { SfActe } );
            }
        }

    }
}
