using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
// for sales force
using GED.Tools.WSDLQualifFinal;
using System.Configuration;


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
                IActe acteprod = new Spirica(actes[i]);
                Dictionary<string[], WsResponse> currentResponse = new Dictionary<string[], WsResponse>();
                currentResponse = await acteprod.sendProd(); // send one "Acte" *** (dic with one element)
                cresponses.Add(currentResponse.Keys.ElementAt(0), currentResponse[currentResponse.Keys.ElementAt(0)]); // get current element
            }
            updateSalesForce(cresponses);
            bool prodState = Spirica.getProdState(); // used before to return prod state
            return Spirica.getListSuccess();
        }// must return boolean

        //update "acte" by "acte" this fucntion consume more time/space than updateSalesForceV1 but fix permission issue
        // method must be splited for each attribute identifier ♣ GERER L'exception de celui la
        public void updateSalesForce(Dictionary<string[],WsResponse> responses){
            // IDS
            string username = ConfigurationManager.AppSettings["loginSF"];
            string passwd = ConfigurationManager.AppSettings["mdpSF"];
        
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
                if (result.size != 0) {
                    SfActe = (Acte__c)result.records[0]; // take the only item selected
                    // update data
                    SfActe.Commentaire_XML__c = string.Join(" ", responses[response.Key].message);
                    SfActe.Statut_du_XML__c = responses[response.Key].status_xml; // <== update status for prod acte and leave it empty in acte
                    if (!responses[response.Key].isSuccessCall) SfActe.fieldsToNull = new String[] { "Date_Envoi_Prod__c" }; // purger la date pour qu'elle ne figure pas dans la Regul
                    SaveResult[] saveResults = SfService.update(new sObject[] { SfActe });
                } // must return one or zero
                    
                //UPDATE PROD ACTE
                Production_Acte__c prodActe = new Production_Acte__c();
                string soqlQueryProdActe = "SELECT Id, Statut_du_XML__c FROM Production_Acte__c WHERE Name = '" + response.Key[1] + "'";
                result = SfService.query(soqlQueryProdActe);
                if (result.size != 0) {
                    prodActe = (Production_Acte__c)result.records[0];
                    prodActe.Statut_du_XML__c = responses[response.Key].status_xml;
                    SaveResult[] saveResults = SfService.update(new sObject[] { prodActe });
                }
            }
        }

        // method to connect to Force API
        private SforceService connect(string username,string passwd){

            SforceService SfService = new GED.Tools.WSDLQualifFinal.SforceService();
            LoginResult loginResult = SfService.login(username, passwd);
            SfService.Url = loginResult.serverUrl;
            SfService.SessionHeaderValue = new SessionHeader();
            SfService.SessionHeaderValue.sessionId = loginResult.sessionId;
            return SfService;
        }

        //Purge all "ACTES" at once
        public void release(){
            // not used
        }
    }
}
