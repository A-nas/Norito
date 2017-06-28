using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Ionic.Zip;

namespace GED.Handlers
{
    public class Export
    {

        public Export(){}

        /// <summary>
        /// liste des dates des situations disponibles
        /// </summary>
        /// <returns></returns>
        public static List<DateTime> GetDateSituation()
        {
            var result = new List<DateTime>();
            var ocon = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnNortiaCA"].ConnectionString);

            const string sql = @"SELECT DateSituation  
                                 FROM   Nortiaca.Tek.DateSituation 
                                 WHERE	DateSituation IS NOT NULL
                                 ORDER BY DateSituation DESC";

            if (ocon.State != ConnectionState.Open) ocon.Open();
            var cmd = new SqlCommand(sql, ocon);
            var dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                result.Add(DateTime.Parse(dr["DateSituation"].ToString()));
            }
            dr.Close(); 
            ocon.Close();


            return result;
        }

        /// <summary>
        /// Nb de document pour l'export situation
        /// </summary>
        /// <param name="appCleSf"></param>
        /// <param name="date"></param>
        /// <param name="assureurId"></param>
        /// <returns></returns>
        public static int GetNbDocSituation(string appCleSf, DateTime date, int? assureurId)
        {
            if (string.IsNullOrEmpty(appCleSf)) return 0;
            var result = 0;
            var ocon = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnNortiaCA"].ConnectionString);

            var sql = @"SELECT  COUNT(*)
                        FROM    ( SELECT   c.pk AS contratpk , c.fkapporteur , ag.PART_AGENCE_CleSalesForce,
				                           tdm.fktypedocs , av.DateAvenant AS datedoc , 
				                           c.FKSOUSCRIPTEUR AS fksouscripteur, c.FKENVELOPPE
				                           , (SELECT env.Assureur
						                        FROM DWNortia.dbo.DIM_Enveloppes env
						                        WHERE	env.Code_Enveloppe = c.FKENVELOPPE AND env.TEK_datesuppression IS NULL
				                           ) AS Assureur
          
                                  FROM     [Nortiaca_MEDIA].[dbo].[CA_MEDIA] m
                                           INNER JOIN Nortiaca.dbo.CA_TYPEDOCS_MEDIA tdm ON tdm.fkmedia = m.pk
                                           INNER JOIN Nortiaca_MEDIA.dbo.Avenant av ON av.ID_Document = m.pk 
                                           INNER JOIN Nortiaca.dbo.ca_contrat c ON c.pk = m.pkvalue
                                           INNER JOIN Nortiaca.dbo.PART_AGENCE ag ON c.FKAPPORTEUR = ag.PART_AGENCE_ID
          
                                  WHERE		(m.VisibleNOL = 1 OR m.VisibleNOL is null) 
					                        AND (m.Original = 1 or m.Original is null)
                               ) t

                        WHERE  Nortiaca.dbo.getShortDate(t.dateDoc) = @date
                               AND t.fktypedocs = 31
                               AND t.PART_AGENCE_CleSalesForce = @appCleSf";

            if (assureurId != null)
                sql += "        AND t.Assureur = @assureur";

            if (ocon.State != ConnectionState.Open) ocon.Open();
            var cmd = new SqlCommand(sql, ocon);
            cmd.Parameters.Add("@appCleSf", SqlDbType.NVarChar).Value = appCleSf;
            cmd.Parameters.Add("@date", SqlDbType.DateTime).Value = date;
            if (assureurId != null)
                cmd.Parameters.Add("@assureur", SqlDbType.Int).Value = assureurId;

            result = int.Parse(cmd.ExecuteScalar().ToString());

            ocon.Close();

            return result;
        }

        /// <summary>
        /// Export situation pdf et zip
        /// </summary>
        /// <param name="appCleSf"></param>
        /// <param name="date"></param>
        /// <param name="assureurId"></param>
        /// <param name="assureurNom"></param>
        /// <param name="zipFormat"></param>
        public static void GetPdfSituation(string appCleSf, DateTime date, int? assureurId, string assureurNom, bool zipFormat = false)
        {
            if (string.IsNullOrEmpty(appCleSf)) return;
            
            var ocon = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnNortiaCA"].ConnectionString);

            var sql = @"select s.nom as souscripteur, td.nom as nomtypedoc, t.* 
                        from   (SELECT	c.pk as contratpk, m.pk, m.nom, m.datas, 
				                        c.fkenveloppe, ag.PART_AGENCE_CleSalesForce, tdm.fktypedocs, 
				                        av.DateAvenant AS datedoc,  c.ncontrat as ncontrat, c.FKSOUSCRIPTEUR AS fksouscripteur
				                        , (SELECT env.Assureur
						                        FROM DWNortia.dbo.DIM_Enveloppes env
						                        WHERE	env.Code_Enveloppe = c.FKENVELOPPE AND env.TEK_datesuppression IS NULL
				                           ) AS Assureur
                                FROM    [Nortiaca_MEDIA].[dbo].[CA_MEDIA] m
                                        INNER JOIN Nortiaca.dbo.CA_TYPEDOCS_MEDIA tdm ON tdm.fkmedia = m.pk
                                        INNER JOIN Nortiaca_MEDIA.dbo.Avenant av ON av.ID_Document = m.pk 
                                        INNER JOIN Nortiaca.dbo.ca_contrat c ON c.pk = m.pkvalue
                                        INNER JOIN Nortiaca.dbo.PART_AGENCE ag ON c.FKAPPORTEUR = ag.PART_AGENCE_ID
                
                                WHERE	(m.VisibleNOL = 1 OR m.VisibleNOL is null) AND (m.Original = 1 or m.Original is null)
                        ) t
                         INNER     JOIN Nortiaca.dbo.ca_souscripteur s ON s.pk = t.fksouscripteur
                         INNER     JOIN Nortiaca.dbo.CA_TYPEDOCS td ON td.id = t.fktypedocs
                         WHERE     Nortiaca.dbo.getShortDate(t.dateDoc) = @date
		                           AND t.fktypedocs = 31
                                   AND t.PART_AGENCE_CleSalesForce = @appCleSf  ";

            if (assureurId != null)
                sql += "        AND t.Assureur = @assureur";

            if (ocon.State != ConnectionState.Open) ocon.Open();
            var cmd = new SqlCommand(sql, ocon);
            cmd.Parameters.Add("@appCleSf", SqlDbType.NVarChar).Value = appCleSf;
            cmd.Parameters.Add("@date", SqlDbType.DateTime).Value = date;
            if (assureurId != null)
                cmd.Parameters.Add("@assureur", SqlDbType.Int).Value = assureurId;



            var dr = cmd.ExecuteReader();
            if (!dr.HasRows)
            {
                HttpContext.Current.Response.Write("Aucun document à télécharger.");
                HttpContext.Current.Response.End();
            }

            var cpt = 0;
            var doc = new TallComponents.PDF.Document();
            dynamic zip = new ZipFile();

            while (dr.Read())
            {
                cpt += 1;
                var b = (byte[])dr["datas"];
                if (zipFormat)
                {
                    dynamic entry = new ZipEntry();
                    var _nomFichier = cpt 
                                    + "_" + dr["souscripteur"] 
                                    + "_" + dr["ncontrat"] 
                                    + "_" + dr["nomtypedoc"].ToString().Replace("(-)", string.Empty) 
                                    + "_" + (DateTime.Parse(dr["datedoc"].ToString()).ToShortDateString().Replace("/", "-"))
                                    + ".pdf";
                    entry = zip.AddEntry(_nomFichier, "\\", b);
                }
                else
                {
                    var ms = new MemoryStream(b);
                    var doc1 = new TallComponents.PDF.Document(ms);
                    doc.Pages.AddRange(doc1.Pages.CloneToArray());
                }

            }

            dr.Close();
            ocon.Close();

            var filename = "-" + date.Year 
                            + "-" + (date.Month.ToString().Length > 1 ? date.Month.ToString() :"0" + date.Month)
                            + "-" + (date.Day.ToString().Length > 1 ? date.Day.ToString() : "0" + date.Month);

            if (zipFormat)
            {
                HttpContext.Current.Response.ContentType = "application/zip";
                HttpContext.Current.Response.AddHeader("content-disposition", "attachment;filename=situations" + filename + ".zip");
                zip.Save(HttpContext.Current.Response.OutputStream);
                zip.Dispose();
            }
            else
            {
                var msout = new MemoryStream();
                doc.Write(msout);
                HttpContext.Current.Response.ContentType = "application/pdf";
                HttpContext.Current.Response.AddHeader("content-disposition", "attachment;filename=situations" + filename + ".pdf");
                HttpContext.Current.Response.BinaryWrite(msout.GetBuffer());
                msout.Close();
            }

        }
        
 
        
    }
}
