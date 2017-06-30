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
            Spirica.showFiles(); // default directory is => /spirica/
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
            List<Acte> actes = Definition.GetListeActes();
            string[] respones = await Production.getInstance().envoyerProd(actes);
            
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
    }
}