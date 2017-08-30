using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
// for sales force
using GED.Tools.WSDLQualifFinal;



namespace GED.Handlers
{
    //SINGLETON
    public class Production
    {
        private static Production refInstance = null;

        //method to get the instance of singleton
        public static Production getInstance(){
                if (refInstance == null){
                    refInstance = new Production();
                }
                return refInstance;
        }

        private Production(){}

        //** method to send a List of 'Acte'
        public async Task<List<string>> envoyerProd(List<Acte> actes){

            int nombreActes = actes.Count;
            Dictionary<string[], WsResponse> cresponses = new Dictionary<string[], WsResponse>();
            for (int i = 0; i < nombreActes; i++){
            // if i pass TRANSTYPE TABLE here as method parameter, The context will depend on the company (unless TRANSTYPE table concerne all companies)
                //Dynamic Dyspatching
                IActe acteprod = new Spirica(actes[i]);
                Dictionary<string[], WsResponse> currentResponse = new Dictionary<string[], WsResponse>();
                currentResponse = await acteprod.sendProd(); // send one "Acte" *** (dic with one element)
                cresponses.Add(currentResponse.Keys.ElementAt(0), currentResponse[currentResponse.Keys.ElementAt(0)]); // get current element
            }
            updateSalesForce(cresponses);
            bool prodState = Spirica.getProdState(); // used before to return prod state
            return Spirica.getListSuccess();
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
            SforceService SfService = new GED.Tools.WSDLQualifFinal.SforceService(); 
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

        //update "acte" by "acte" this fucntion consume more time/space than updateSalesForceV1 but fix permission issue
        // method must be splited for each attribute identifier
        public void updateSalesForce(Dictionary<string[],WsResponse> responses){
            // IDS
            string username = "noluser@nortia.fr.nqualif";//#
            string passwd = "nortia01";//#
        
            SforceService SfService = new GED.Tools.WSDLQualifFinal.SforceService();
            LoginResult loginResult = SfService.login(username, passwd);
            SfService.Url = loginResult.serverUrl;
            SfService.SessionHeaderValue = new SessionHeader();
            SfService.SessionHeaderValue.sessionId = loginResult.sessionId;

            foreach(KeyValuePair<string[], WsResponse> response in responses){
                //UPDATE ACTE
                Acte__c SfActe = new Acte__c();
                //string soqlQuery = "SELECT Id, Commentaire_Interne__c, Statut_du_XML__c FROM Acte__c WHERE Name = '" + response.Key + "'"; OLD
                string soqlQueryActe = "SELECT Id, Commentaire_XML__c, Statut_du_XML__c FROM Acte__c WHERE Name = '" + response.Key[0] + "'";
                QueryResult result = SfService.query(soqlQueryActe);
                if(result.size != 0) // must return one or zero
                    SfActe = (Acte__c)result.records[0]; // take the only item selected
                // update data
                SfActe.Commentaire_XML__c = string.Join(" ", responses[response.Key].message);
                SfActe.Statut_du_XML__c = responses[response.Key].status_xml; // <== update status for prod acte and leave it empty in acte
                SaveResult[] saveResults = SfService.update(new sObject[] { SfActe } );

                //UPDATE PROD ACTE
                Production_Acte__c prodActe = new Production_Acte__c();
                string soqlQueryProdActe = "SELECT Id, Statut_du_XML__c FROM Production_Acte__c WHERE Name = '" + response.Key[1] + "'";
                result = SfService.query(soqlQueryProdActe);
                if (result.size != 0)
                    prodActe = (Production_Acte__c)result.records[0];
                prodActe.Statut_du_XML__c = responses[response.Key].status_xml;
                saveResults = SfService.update(new sObject[] { prodActe });

            }
        }

    }
}
