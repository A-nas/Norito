using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Office.Interop.Word;
using Newtonsoft.Json;
using GED.Handlers;
// call web service (not tested yet)
using System.Net.Http;
using System.Net.Http.Headers;
// for webProxyClass
using System.Net;
//for Burp suite debug
using System.Security.Cryptography.X509Certificates;
//for inserting data into sql server
using System.Data.SqlClient;
//Json read
using Newtonsoft.Json.Linq;
//Sales Force connexion
using GED.Tools.WSDLQualifFinal;

namespace Tests.Interfaces
{

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            List<indexationged.IntegrationDocument> listIntegrationDocument = new List<indexationged.IntegrationDocument>();

            indexationged.IntegrationDocument doc1 = new indexationged.IntegrationDocument();
            doc1.Index = 0;
            doc1.ID_ActeSalesforce = "a048E000004QWL4QAO";
            doc1.NomDocument = "TestPDF";
            doc1.ExtensionDocument = "pdf";
            doc1.TypeActe = "Mouvements Administratifs";
            doc1.TypeDocument = "Avis d'imposition";
            doc1.VisibleNOL = true;

            using (FileStream fs = File.Open(@"C:\Temp\fille.pdf", FileMode.Open))
            {
                fs.Position = 0;
                byte[] datas = new BinaryReader(fs).ReadBytes(int.Parse(fs.Length.ToString()));
                fs.Position = 0;
                fs.Close();

                doc1.DataDocument = datas;
            }
            listIntegrationDocument.Add(doc1);

            indexationged.IntegrationDocument doc2 = new indexationged.IntegrationDocument();
            doc2.Index = 1;
            doc2.ID_ActeSalesforce = "a048E000004QWL4QAO";
            doc2.NomDocument = "TestPNG";
            doc2.ExtensionDocument = "png";
            doc2.TypeActe = "Mouvements Administratifs";
            doc2.TypeDocument = "Justificatif de domicile";
            doc2.VisibleNOL = true;

            using (FileStream fs = File.Open(@"C:\Temp\souris.png", FileMode.Open))
            {
                fs.Position = 0;
                byte[] datas = new BinaryReader(fs).ReadBytes(int.Parse(fs.Length.ToString()));
                fs.Position = 0;
                fs.Close();

                doc2.DataDocument = datas;
            }
            listIntegrationDocument.Add(doc2);

            indexationged.IndexationGED ws = new indexationged.IndexationGED();
            indexationged.ListeIntegrationDocumentResponse ret = ws.IntegrationListeDocument(listIntegrationDocument.ToArray(), "NSAS");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (FileStream imageStream = new FileStream(@"C:\Temp\fifille.jpg", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (FileStream file = new FileStream(@"C:\Temp\fifille.pdf", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    imageStream.Position = 0;
                    byte[] datas = new BinaryReader(imageStream).ReadBytes(int.Parse(imageStream.Length.ToString()));
                    imageStream.Position = 0;

                    using (MemoryStream ms = new MemoryStream(ConvertImageToPDF(datas)))
                    {
                        ms.WriteTo(file);
                    }
                }
            }
        }


        private void button3_Click(object sender, EventArgs e)
        {
            using (FileStream imageStream = new FileStream(@"C:\Temp\Cahier des charges.docx", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (FileStream file = new FileStream(@"C:\Temp\Cahier des charges.pdf", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    imageStream.Position = 0;
                    byte[] datas = new BinaryReader(imageStream).ReadBytes(int.Parse(imageStream.Length.ToString()));
                    imageStream.Position = 0;

                    using (MemoryStream ms = new MemoryStream(ConvertWordToPDF(datas)))
                    {
                        ms.WriteTo(file);
                    }
                }
            }
        }

        private static MemoryStream ConvertImageToPDF(Stream stFileImage)
        {
            MemoryStream msFilePDF = new MemoryStream();

            try
            {
                iTextSharp.text.Document document = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 25, 25, 25, 25);

                PdfWriter.GetInstance(document, msFilePDF);
                document.Open();

                var image = iTextSharp.text.Image.GetInstance(stFileImage);

                if (image.Height > iTextSharp.text.PageSize.A4.Height - 25)
                {
                    image.ScaleToFit(iTextSharp.text.PageSize.A4.Width - 25, iTextSharp.text.PageSize.A4.Height - 25);
                }
                else if (image.Width > iTextSharp.text.PageSize.A4.Width - 25)
                {
                    image.ScaleToFit(iTextSharp.text.PageSize.A4.Width - 25, iTextSharp.text.PageSize.A4.Height - 25);
                }
                image.Alignment = iTextSharp.text.Image.ALIGN_MIDDLE;

                document.Add(image);

                document.Close();

                using (FileStream file = new FileStream(@"C:\Temp\fifille.pdf", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    msFilePDF.WriteTo(file);
                }

            }
            catch (Exception ex)
            {
                msFilePDF = null;
            }

            return msFilePDF;
        }

        void ConvertJPG2PDF(string jpgfile, string pdf)
        {
            iTextSharp.text.Document document = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 25, 25, 25, 25);
            using (Stream stream = new FileStream(pdf, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                PdfWriter.GetInstance(document, stream);
                document.Open();
                using (FileStream imageStream = new FileStream(jpgfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(imageStream);
                    if (image.Height > iTextSharp.text.PageSize.A4.Height - 25)
                    {
                        image.ScaleToFit(iTextSharp.text.PageSize.A4.Width - 25, iTextSharp.text.PageSize.A4.Height - 25);
                    }
                    else if (image.Width > iTextSharp.text.PageSize.A4.Width - 25)
                    {
                        image.ScaleToFit(iTextSharp.text.PageSize.A4.Width - 25, iTextSharp.text.PageSize.A4.Height - 25);
                    }
                    image.Alignment = iTextSharp.text.Image.ALIGN_MIDDLE;
                    document.Add(image);
                }

                document.Close();
            }
        }

        void ConvertJPG2PDF(Stream imageStream, string pdf)
        {
            iTextSharp.text.Document document = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 25, 25, 25, 25);
            using (Stream stream = new FileStream(pdf, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                PdfWriter.GetInstance(document, stream);
                document.Open();

                    iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(imageStream);
                    if (image.Height > iTextSharp.text.PageSize.A4.Height - 25)
                    {
                        image.ScaleToFit(iTextSharp.text.PageSize.A4.Width - 25, iTextSharp.text.PageSize.A4.Height - 25);
                    }
                    else if (image.Width > iTextSharp.text.PageSize.A4.Width - 25)
                    {
                        image.ScaleToFit(iTextSharp.text.PageSize.A4.Width - 25, iTextSharp.text.PageSize.A4.Height - 25);
                    }
                    image.Alignment = iTextSharp.text.Image.ALIGN_MIDDLE;
                    document.Add(image);

                document.Close();
            }
        }

        private byte[] ConvertJPG2PDF(Stream imageStream)
        {
            iTextSharp.text.Document document = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 25, 25, 25, 25);
            MemoryStream memStream = new MemoryStream();

            PdfWriter wr =PdfWriter.GetInstance(document, memStream);
                document.Open();

                iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(imageStream);
                if (image.Height > iTextSharp.text.PageSize.A4.Height - 25)
                {
                    image.ScaleToFit(iTextSharp.text.PageSize.A4.Width - 25, iTextSharp.text.PageSize.A4.Height - 25);
                }
                else if (image.Width > iTextSharp.text.PageSize.A4.Width - 25)
                {
                    image.ScaleToFit(iTextSharp.text.PageSize.A4.Width - 25, iTextSharp.text.PageSize.A4.Height - 25);
                }
                image.Alignment = iTextSharp.text.Image.ALIGN_MIDDLE;
                document.Add(image);

                document.Close();

             return memStream.ToArray();
        }

        private static byte[] ConvertImageToPDF(byte[] dataImage)
        {
            byte[] dataPDF=null;

            try
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(dataImage);

                    if (image.Height > (iTextSharp.text.PageSize.A4.Height - 25) && image.Height < image.Width)
                        image.RotationDegrees = 90;

                    if (image.Height > iTextSharp.text.PageSize.A4.Height - 25)
                    {
                        image.ScaleToFit(iTextSharp.text.PageSize.A4.Width - 25, iTextSharp.text.PageSize.A4.Height - 25);
                    }
                    else if (image.Width > iTextSharp.text.PageSize.A4.Width - 25)
                    {
                        image.ScaleToFit(iTextSharp.text.PageSize.A4.Width - 25, iTextSharp.text.PageSize.A4.Height - 25);
                    }
                    image.Alignment = iTextSharp.text.Image.ALIGN_MIDDLE;

                    iTextSharp.text.Document documentPDF = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 25, 25, 25, 25);
                    PdfWriter.GetInstance(documentPDF, memStream);
                    documentPDF.Open();
                    documentPDF.Add(image);
                    documentPDF.Close();

                    dataPDF = memStream.ToArray();
                }
            }
            catch(Exception)
            {
                dataPDF = null;
            }
            return dataPDF;
        }


        private static byte[] ConvertWordToPDF(byte[] dataWord)
        {
            byte[] dataPDF = null;

            Microsoft.Office.Interop.Word.Application appWord = new Microsoft.Office.Interop.Word.Application();

            string tmpFileWord = Path.GetTempFileName();
            FileStream tmpFileStream = File.OpenWrite(tmpFileWord);
            tmpFileStream.Write(dataWord, 0, dataWord.Length);
            tmpFileStream.Close();

            Microsoft.Office.Interop.Word.Document wordDocument = appWord.Documents.Open(tmpFileWord);

            string tmpFilePDF = Path.GetTempFileName();
            wordDocument.ExportAsFixedFormat(tmpFilePDF, WdExportFormat.wdExportFormatPDF);

            using (FileStream fs = new FileStream(tmpFilePDF, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Position = 0;
                dataPDF = new BinaryReader(fs).ReadBytes(int.Parse(fs.Length.ToString()));
                fs.Position = 0;
            }

            return dataPDF;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string test = "last";
            Acte a = new Acte();
            List<Acte> la = new List<Acte>();
            // remplissage
                //premier acte
            a.ReferenceInterne = "referenceInterne";
            a.Frais = 0;
            Repartition r1 = new Repartition();
            r1.CodeISIN = "support01";
            r1.TypeRepartition = "%";
            r1.ValeurRepartition = 50;

            Repartition r2 = new Repartition();
            r2.CodeISIN = "support02";
            r2.TypeRepartition = "€";
            r2.ValeurRepartition = 100000;

            a.ListeSupportDesinvestir = new List<Repartition>(); // à.completer.
            a.ListeSupportDesinvestir.Add(r1);                   // ............
            a.ListeSupportInvestir = new List<Repartition>();    // ...par.la...
            a.ListeSupportInvestir.Add(r2);                      // ............
            a.ListeDocument = new List<DocumentProduction>();    // ...suite....
            a.Commentaire = "ceci est un commentaire";

            DocumentProduction doc1 = new DocumentProduction();
            DocumentProduction doc2 = new DocumentProduction();
            doc1.ID_DocumentNortia = 325;
            doc2.ID_DocumentNortia = 477;
            a.ListeDocument.Add(doc1);
            a.ListeDocument.Add(doc2);

            //deuxiemmme acte

            Acte a2 = new Acte();
            a2.ReferenceInterne = "referenceInterne";
            a2.Frais = 0;
            Repartition r12 = new Repartition();
            r12.CodeISIN = "support01";
            r12.TypeRepartition = "%";
            r12.ValeurRepartition = 50;

            Repartition r22 = new Repartition();
            r22.CodeISIN = "support02";
            r22.TypeRepartition = "€";
            r22.ValeurRepartition = 100000;

            a2.ListeSupportDesinvestir = new List<Repartition>(); // à.completer.
            a2.ListeSupportDesinvestir.Add(r22);                   // ............
            a2.ListeSupportInvestir = new List<Repartition>();    // ...par.la...
            a2.ListeSupportInvestir.Add(r22);                      // ............
            a2.ListeDocument = new List<DocumentProduction>();    // ...suite....
            a2.Commentaire = "ceci est un commentaire";

            DocumentProduction doc11 = new DocumentProduction();
            DocumentProduction doc22 = new DocumentProduction();
            doc11.ID_DocumentNortia = 304;
            doc22.ID_DocumentNortia = 306;
            a2.ListeDocument.Add(doc11);
            a2.ListeDocument.Add(doc22);

            la.Add(a);
            la.Add(a2);


            // fin remplissage

            if (test.Equals("first"))
            {
                
                //serialisation
                JsonSerializerSettings jsonSetting = new JsonSerializerSettings
                {
                    // ces options sont valable aussi pour les sous objets
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    ContractResolver = new ShouldSerializeContractResolver()
                };


                // display json on messagebox
                string json = JsonConvert.SerializeObject(a, jsonSetting);
                MessageBox.Show(json);

                // write json to a file 
                string mydocpath =
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // (mes documents)
                using (StreamWriter outputFile = new StreamWriter(mydocpath + @"\SpiricaJson.txt"))
                {
                    outputFile.WriteLine(json);
                }
            }else // for a liste of Actes
            {
                //test with final class
                //Spirica spirica = new Spirica(la);
                 // IActe acte = new Spirica(la);
                //  string json = acte.genJSON();

                string mydocpath =
                            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // (mes documents)
                using (StreamWriter outputFile = new StreamWriter(mydocpath + @"\SpiricaJson.txt"))
                {
                    //outputFile.WriteLine(json);
                }
                //MessageBox.Show(json);
                
                Definition.setProgressBar(progressBar1);
                //acte.sendProd();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //check wether file has uploaded
            //Spirica.showFiles(); // default directory is => /spirica/
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            Test t = new Test();
            t.IntegrerCAT_BIS();
        }

        private async void button7_Click(object sender, EventArgs e)
        {
            try
            {
                //string url = "https://httpbin.org/post";

                HttpClientHandler handler = new HttpClientHandler()
                {
                    Proxy = new WebProxy("http://192.168.10.238:808"),
                    UseProxy = true,
                };
                // set certificat authority
                handler.ClientCertificateOptions = ClientCertificateOption.Automatic;

                //handler.SslProtocols = SslProtocols.Tls12;
                //handler.ClientCertificates.Add(new X509Certificate2("cert.crt"));

                string url = "https://api-recette.spirica.fr/sylveaRS/v1/contrats/113100096/arbitrages";
                HttpClient client = new HttpClient();//handler
                var byteArray = Encoding.ASCII.GetBytes("NORTIAWS:a*yixw9.8sq");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                //set request headers
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Accept-Charset", "UTF-8");

                var requestContent = new MultipartFormDataContent();
                //    here you can specify boundary if you need---^ (no thanks no boundaries)

                // read bytes 
                var pdfdemande = new ByteArrayContent(File.ReadAllBytes(@"C:\Users\alaghouaouta\Desktop\LastTestUntilRefactoring\demande.pdf"));
                var pdfArbitrage = new ByteArrayContent(File.ReadAllBytes(@"C:\Users\alaghouaouta\Desktop\LastTestUntilRefactoring\dossier_arbitrage.pdf"));
                var json = new ByteArrayContent(File.ReadAllBytes(@"C:\Users\alaghouaouta\Desktop\LastTestUntilRefactoring\fluxJson3.json"));
                // end read bytes

                //var jsonContent = new ByteArrayContent("str");
                json.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                pdfdemande.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                pdfArbitrage.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

                // add all content
                requestContent.Add(json, "arbitrage");
                requestContent.Add(pdfdemande, "file", "demande.pdf");
                requestContent.Add(pdfArbitrage, "file", "dossier_arbitrage.pdf");

                var message = await client.PostAsync(url, requestContent);
                var content = await message.Content.ReadAsStringAsync();

                writeIntoFile(content);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }


        // a effacer 
        public async static Task<string> GetRequest<T>()
        {
            //after //string url = "https://NORTIAWS:a*yixw9.8sq@api-recette.spirica.fr/sylveaRS/v1/contrats/113110000/arbitrages";
            string url = "https://httpbin.org/post";
            try
            {
                    // preparing data te bo sent
                //HttpClient client = new HttpClient();
                //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")); //accepte Header
                //client.DefaultRequestHeaders.Add("Content-Type", "application/json");

                //var arbitrage = new StringContent("arbitrageAsJson"); //StringContent herits from HttpContent used in PostAsync
                //var sending = await client.PostAsync(url, arbitrage);


                    //get response
                //var contents = await sending.Content.ReadAsStringAsync();
                //return contents;
                    //return Newtonsoft.Json.JsonConvert.DeserializeObject<string>(contents);
                    //HttpResponseMessage response = await client.GetAsync(url);
                    //string json = await response.Content.ReadAsStringAsync();
                    //return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);


                // test 2 this must be working.
                HttpRequestMessage reqMessage;
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("https://httpbin.org/post");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://httpbin.org/post");

                byte[] bytes = System.IO.File.ReadAllBytes(@"C:\Users\alaghouaouta\Desktop\Nouveau dossier\test.pdf");
                string file = Convert.ToBase64String(bytes);
                request.Content = new StringContent("{\"name\":\"John Doe\", \"age\":33 \"file\":[\"" + file + ";filename=demande.pdf;application/octet-stream\";\"" + file + ";filename=demande.pdf;application/octet-stream\"]}",
                                        Encoding.UTF8,
                                        "application/json");//CONTENT-TYPE header
                
                HttpResponseMessage response = await client.SendAsync(request);
                string contents = await response.Content.ReadAsStringAsync();
                writeIntoFile(contents);
                return contents;
            }
            catch
            {
                return default(string);
            }
        }


        // a implementer
        public static async Task<string> Upload()
        {
            //static data
            string  url = "https://httpbin.org/post";

            HttpClient client = new HttpClient();
            var requestContent = new MultipartFormDataContent();
            //    here you can specify boundary if you need---^
            
            // read bytes
            var pdfdemande = new ByteArrayContent(System.IO.File.ReadAllBytes(@"C:\Users\Anas\Desktop\demande.pdf"));
            var pdfArbitrage = new ByteArrayContent(System.IO.File.ReadAllBytes(@"C:\Users\Anas\Desktop\dossier_arbitrage.pdf"));
            var json = new ByteArrayContent(System.IO.File.ReadAllBytes(@"C:\Users\Anas\Desktop\fluxJson.json"));
            // end read bytes

            //var jsonContent = new ByteArrayContent("str");
            json.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            pdfdemande.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            pdfArbitrage.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

            // add all content
            requestContent.Add(json);
            requestContent.Add(pdfdemande);
            requestContent.Add(pdfArbitrage);

            var message = await client.PostAsync(url, requestContent);
            var content = await message.Content.ReadAsStringAsync();
            return content;
        }


        // write string to file (mes document)
        private static void writeIntoFile(string toWrite)
        {
            string mydocpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // (mes documents)
            using (StreamWriter outputFile = new StreamWriter(mydocpath + @"\outGED.txt"))
            {
                outputFile.WriteLine(toWrite);
            }
        }

        private async void button8_Click(object sender, EventArgs e)
        {
            // liste des actes attendues


            /*
            List<Acte> actes = Definition.GetListeActes();
            string[] respones = await Production.getInstance().envoyerProd(actes);
            List<Acte> listeActeSucces = new List<Acte>();
            for (int i = 0; i < respones.Length; i++)
                 if (Convert.ToBoolean(JObject.Parse(respones[i])["succes"])) listeActeSucces.Add(actes[i]);
            */ 
            
              
            /*List<Acte> actes = Definition.GetListeActes();
                // prod
                int nombreActes = actes.Count();
                string[] response = new string[nombreActes];
            for (int i = 0; i < nombreActes; i++)
            {
                IActe acteprod = new Spirica(actes[i]);
                //IActe acteprod = (IActe) actes[i]; // cast avec du code 
                response[i] = (await acteprod.sendProd());
            }*/



        }



        //a enlever
        private void button9_Click(object sender, EventArgs e)
        {
            // SUPPORTS TABLE FILL !!
            try
            {
                //String query = "INSERT INTO dbo.SUPPORT_TRANSTYPE ([Code_Support],[Code_ISIN],[libelle_support],[Type],[Code_Compagnie]) VALUES (@codeSupport,@CodeISIN,@LibelleSupport, @Type, 'SPI' )";
                string query = "INSERT INTO dbo.SUPPORT_TRANSTYPE ([Code_Support],[Code_ISIN],[Code_Compagnie]) VALUES (@codeSupport,@CodeISIN, 'SPI' )";
                // connexion string a changeeeeeeeeeer

                Definition.connexionQualif.Open();
                StreamReader rd = new StreamReader(@"C:\Users\alaghouaouta\Desktop\Nouveau dossier\FichierEnvoyésParSpirica\message 4\Supports_gamme_PRIVATE_RECETTE_21072017.csv");

                while (!rd.EndOfStream)
                {
                    string[] splits = rd.ReadLine().Split(';');
                    // connexion parameter
                        {
                                SqlCommand command = new SqlCommand(query, Definition.connexionQualif);
                                command.Parameters.AddWithValue("@codeSupport", splits[0].ToString());
                                command.Parameters.AddWithValue("@CodeISIN", splits[1].ToString());
                                // run !
                                command.ExecuteNonQuery();
                        }
                }
                Definition.connexionQualif.Close();
            }
            catch(Exception ex) { }

            

        }

        private void button10_Click(object sender, EventArgs e)
        {
            //List<GED.Handlers.Acte> HActes = Definition.GetListeActes();
            genererprodLocal.GenerationProdSoapClient test = new genererprodLocal.GenerationProdSoapClient();
            //test.GenererProd("TEST", "SPI", actes.ToArray() , "Scan", true, "");
            genererprodLocal.Acte[] Sactes = new genererprodLocal.Acte[1];
            Sactes[0] = new genererprodLocal.Acte();
                // sub object
                {
                genererprodLocal.Repartition rep01 = new genererprodLocal.Repartition
                {
                        CodeISIN = "FR0010696765",
                        TypeRepartition = "%",
                        ValeurRepartition = 30
                    };

                genererprodLocal.Repartition rep02 = new genererprodLocal.Repartition
                {
                        CodeISIN = "FR0007071378",
                        TypeRepartition = "%",
                        ValeurRepartition = 100
                    };

                genererprodLocal.DocumentProduction doc01 = new genererprodLocal.DocumentProduction
                {
                        ID_DocumentNortia = 1636367,
                        ID_DocumentSalesForce = "a098E000003hgUxQAI",
                        NbPage = 1
                };

                genererprodLocal.DocumentProduction doc02 = new genererprodLocal.DocumentProduction
                {
                    ID_DocumentNortia = 1636370,
                    ID_DocumentSalesForce = "a098E000003hgUxQAI",
                    NbPage = 1
                };


                /*
                GenererProd.DocumentProduction doc02 = new GenererProd.DocumentProduction
                {
                        ID_DocumentNortia = 38391,
                        ID_DocumentSalesForce = "idSalesForce"
                    };*/
                //remplissage

                Sactes[0].ListeSupportDesinvestir = new genererprodLocal.Repartition[1];
                Sactes[0].ListeSupportInvestir = new genererprodLocal.Repartition[1];
                Sactes[0].ListeSupportDesinvestir[0] = new genererprodLocal.Repartition();
                Sactes[0].ListeSupportInvestir[0] = new genererprodLocal.Repartition();

                Sactes[0].ListeSupportDesinvestir[0] = rep01;
                Sactes[0].ListeSupportInvestir[0] = rep02;
           


                Sactes[0].ListeDocument = new genererprodLocal.DocumentProduction[2];
                Sactes[0].ListeDocument[0] = new genererprodLocal.DocumentProduction();
                Sactes[0].ListeDocument[1] = new genererprodLocal.DocumentProduction();
                // Sactes[0].ListeDocument[1] = new GenererProd.DocumentProduction();
                Sactes[0].ListeDocument[0] = doc01;
                Sactes[0].ListeDocument[1] = doc02;
                //Sactes[0].ListeDocument[1] = doc02;
                //

                Sactes[0].Frais = 0f;
                Sactes[0].Commentaire = "un commentaire";
                
                Sactes[0].NomType = "Arbitrage";
                Sactes[0].NomActeAdministratif = "";
                Sactes[0].ReferenceInterne = "ACT000407542";
                Sactes[0].NomCompletSouscripteurs = "";
                Sactes[0].NumContrat = "112900052";
                Sactes[0].CodeApporteur = "NOR100055";
                Sactes[0].NomApporteur = "TEISSEDRE ET ASSOCIES GESTION DE PATRIMOINE";
                Sactes[0].MontantBrut = 8253.12f;
                Sactes[0].TypeFrais = "%";
                Sactes[0].Frais = 1;
                Sactes[0].ID_ProfilCompagnie = "";
                Sactes[0].NomEnveloppe = "PRIVATE VIE";
                Sactes[0].IsTraitementEdi = true;
                Sactes[0].DateCreation = DateTime.Now;
                Sactes[0].DateAcquisition = DateTime.Now;
                Sactes[0].DateEnvoiProduction = DateTime.Now; // supposed
                Sactes[0].Commentaire = "";
                Sactes[0].InvestissementImmediat = false;
                Sactes[0].Regul = true;
                Sactes[0].isSigned = true;
                Sactes[0].prodActeID = "PD-0066346";
            }

                test.GenererProd("TEST", "SPI", Sactes, "Scan", true, "");
            /*
            //deuxiemmme acte
            // sub object
            {

                Repartition rep01 = new Repartition
                {
                    CodeISIN = "FEURO",
                    TypeRepartition = "%",
                    ValeurRepartition = 50
                };

                Repartition rep02 = new Repartition
                {
                    CodeISIN = "SCPI00003719",
                    TypeRepartition = "%",
                    ValeurRepartition = 50
                };

                DocumentProduction doc01 = new DocumentProduction
                {
                    ID_DocumentNortia = 890350,
                    ID_DocumentSalesForce = "idSalesForce2"
                };

                DocumentProduction doc02 = new DocumentProduction
                {
                    ID_DocumentNortia = 38391,
                    ID_DocumentSalesForce = "idSalesForce1"
                };

                //==
                DetailPiece piece01 = new DetailPiece
                {
                    nomFichier = "demande.pdf",
                    typeFicher = "demande_arbitrage" // penser a la modification des types
                };

                DetailPiece piece02 = new DetailPiece
                {
                    nomFichier = "dossier_arbitrage.pdf",
                    typeFicher = "dossier_arbitrage_signe_electroniquement" // penser a la modification des types
                };

                DetailPiece piece03 = new DetailPiece
                {
                    nomFichier = "avenant_support.pdf",
                    typeFicher = "avenant_support" // penser a la modification des types
                };
                //==
                la.Add(new Acte
                {
                    ReferenceInterne = "TEST_FINAL02",
                    ListeSupportInvestir = { rep01, rep02 },
                    Commentaire = "un commentaire",
                    //pieces = { piece01, piece02, piece03 },
                    ListeDocument = { doc01, doc02 },
                    NumContrat = "113100096"
                });
            }


            //3eme acte
            // sub object
            {

                Repartition rep02 = new Repartition
                {
                    CodeISIN = "FEURO",
                    TypeRepartition = "%",
                    ValeurRepartition = 100
                };

                DocumentProduction doc01 = new DocumentProduction
                {
                    ID_DocumentNortia = 890350,
                    ID_DocumentSalesForce = "idSalesForce2"
                };

                DocumentProduction doc02 = new DocumentProduction
                {
                    ID_DocumentNortia = 38391,
                    ID_DocumentSalesForce = "idSalesForce1"
                };

                //==
                DetailPiece piece01 = new DetailPiece
                {
                    nomFichier = "demande.pdf",
                    typeFicher = "demande_arbitrage" // penser a la modification des types
                };

                DetailPiece piece02 = new DetailPiece
                {
                    nomFichier = "dossier_arbitrage.pdf",
                    typeFicher = "dossier_arbitrage_signe_electroniquement" // penser a la modification des types
                };
                //==
                la.Add(new Acte
                {
                    ReferenceInterne = "TEST_FINAL03",
                    ListeSupportInvestir = { rep02 },
                    //ListeSupportDesinvestir = { rep01 },
                    Commentaire = "un commentaire",
                    ListeDocument = { doc01, doc02 },
                    Frais = 0.5f,
                    //pieces = { piece01, piece02 },
                    NumContrat = "113100096"
                });
            }


            //4eme acte
            // sub object
            {
                Repartition rep01 = new Repartition
                {
                    CodeISIN = "FEURO",
                    TypeRepartition = "%",
                    ValeurRepartition = 100
                };

                DocumentProduction doc01 = new DocumentProduction
                {
                    ID_DocumentNortia = 890350,
                    ID_DocumentSalesForce = "idSalesForce2"
                };

                DocumentProduction doc02 = new DocumentProduction
                {
                    ID_DocumentNortia = 38391,
                    ID_DocumentSalesForce = "idSalesForce1"
                };

                //==
                DetailPiece piece01 = new DetailPiece
                {
                    nomFichier = "demande.pdf",
                    typeFicher = "demande_arbitrage" // penser a la modification des types
                };

                DetailPiece piece02 = new DetailPiece
                {
                    nomFichier = "dossier_arbitrage.pdf",
                    typeFicher = "dossier_arbitrage_signe_electroniquement" // penser a la modification des types
                };
                //==
                la.Add(new Acte
                {
                    ReferenceInterne = "TEST_FINAL04",
                    ListeSupportInvestir = { rep01 },
                    Commentaire = "un commentaire",
                    Frais = 0.5f,
                    //pieces = { piece01, piece02 },
                    ListeDocument = { doc01, doc02 },
                    NumContrat = "113100096"
                });
            }*/


        }

        private void button11_Click(object sender, EventArgs e)
        {
        
        }

        private void button12_Click(object sender, EventArgs e)
        {
            //## this fucntion must after all manage exceptions in case if we can't connect to Force.com API
            // fetch for all actes data list
            List<Acte> actes = Definition.GetListeActes(); // data for testing
            string[] idActes = actes.Select(p => p.ReferenceInterne).ToArray(); // extract ids actes
            string idList = "'" + String.Join("','", idActes) + "'"; // construct the part of 'in' query string clause
            string soqlQuery = "SELECT Name, Commentaire_Interne__c, Statut_du_XML__c FROM Acte__c where Name in ("+ idList +")";

            string username = "noluser@nortia.fr.nqualif";//
            string passwd = "nortia01";//

            SforceService SfService = new GED.Tools.WSDLQualifFinal.SforceService(); // call ws
            Dictionary<string, string> dictionnaire = new Dictionary<string, string>();
            try
            {
                LoginResult loginResult = SfService.login(username, passwd);
                SfService.Url = loginResult.serverUrl;
                SfService.SessionHeaderValue = new SessionHeader();
                SfService.SessionHeaderValue.sessionId = loginResult.sessionId;

                QueryResult result = SfService.query(soqlQuery);
                // array to alter/safe
                Acte__c[] SfActes = new Acte__c[result.size];

                for(int i = 0; i<result.size; i++){
                    // cast data
                    SfActes[i] = (Acte__c)result.records[i];
                    //update list //updating current cell //extract here message format and status
                    SfActes[i].Commentaire_Interne__c += "\n"+dictionnaire[SfActes[i].Name];
                    SfActes[i].Statut_du_XML__c = dictionnaire[SfActes[i].Name];
                    MessageBox.Show("data retrived ==> " + SfActes[i].Commentaire_Interne__c + " ; " + SfActes[i].Statut_du_XML__c);
                }
                // save update
                SaveResult[] saveResultsV2 = SfService.update( SfActes );// deplcaer vers la fin
            }
            catch (Exception ex)
            {
                SfService = null;
                throw (ex); // you shall not pass
            }

        }
    }
}