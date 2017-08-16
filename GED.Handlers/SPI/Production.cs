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


        //method to update salesForce records (return complex object)
        public void updateSalesForce(Dictionary<string,WsResponse> responses){

            string[] idActes = responses.Keys.ToArray();//actes.Select(p => p.ReferenceInterne).ToArray(); // extract only keys (ids)
            string idList = "'" + String.Join("','", idActes) + "'";
            string soqlQuery = "SELECT Id, Name, Commentaire_Interne__c, Statut_du_XML__c FROM Acte__c WHERE Name in (" + idList + ")";

            string username = "noluser@nortia.fr.nqualif";//#
            string passwd = "nortia01";//#

            SforceService SfService = new GED.Tools.WSDLQualif.SforceService(); // call ws
            //Dictionary<string, string> dictionnaire = new Dictionary<string, string>();

            try
            {
                LoginResult loginResult = SfService.login(username, passwd);
                SfService.Url = loginResult.serverUrl;
                SfService.SessionHeaderValue = new SessionHeader();
                SfService.SessionHeaderValue.sessionId = loginResult.sessionId;

                QueryResult result = SfService.query(soqlQuery);
                Acte__c[] SfActes = new Acte__c[result.size];

                for (int i = 0; i < result.size; i++)
                {
                    // cast data
                    SfActes[i] = (Acte__c)result.records[i];
                    // update data
                    string retMessage = string.Join(" ", responses[SfActes[i].Name].message);
                    SfActes[i].Commentaire_Interne__c += retMessage;
                    SfActes[i].Statut_du_XML__c = responses[SfActes[i].Name].status_xml;
                }
                // save update
                SaveResult[] saveResults = SfService.update(SfActes);
            }
            catch (Exception ex)
            {
                //"Unable to create/update fields: Name. Please check the security settings of this field and verify that it is read/write for your profile or permission set.";
                SfService = null;
                //throw (ex); // you shall not pass
            }
        }

    }
}
