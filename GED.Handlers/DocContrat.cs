using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace GED.Handlers
{
    public class DisplayedDoc
    {
        #region ---------- properties ----------

        public int MediaPk { get; set; }
        public int CatRank { get; set; }
        public int CatPk { get; set; }
        public string CatNom { get; set; }
        public string MediaNom { get; set; }
        public DateTime DateDoc { get; set; }
        public int Fk { get; set; }
        public string TypeDoc { get; set; }

        #endregion

        public DisplayedDoc()
        {
        }

        public DisplayedDoc(int mediaPk, int catRank, int catPk, string catNom, string mediaNom, DateTime dateDoc, int fk, string typeDoc)
        {
            this.MediaPk = mediaPk;
            this.CatRank = catRank;
            this.CatPk = catPk;
            this.CatNom = catNom;
            this.MediaNom = mediaNom;
            this.DateDoc = dateDoc;
            this.Fk = fk;
            this.TypeDoc = typeDoc;
        }
    }

    public class DisplayedDocMvt :  DisplayedDoc
    {
        #region ---------- properties ----------

        public DateTime DateMvt { get; set; }
        public decimal Montant { get; set; }
        public string RefInterne { get; set; }

        #endregion

        public DisplayedDocMvt()
        {
        }

        public DisplayedDocMvt(int mediaPk, int catRank, int catPk, string catNom, string mediaNom, DateTime dateDoc, int fk, string typeDoc, DateTime dateMvt, Decimal montant, string refInterne)
        {
            this.MediaPk = mediaPk;
            this.CatRank = catRank;
            this.CatPk = catPk;
            this.CatNom = catNom;
            this.MediaNom = mediaNom;
            this.DateDoc = dateDoc;
            this.Fk = fk;
            this.TypeDoc = typeDoc;
            this.DateMvt = dateMvt;
            this.Montant = montant;
            this.RefInterne = refInterne;
        }




        public static DataTable GetDocumentContrat(string appCleSf, string contratCleSf, string catPkContrats, string catPkSous, string excludedCat)
        {
            var _ocon = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnNortiaCA"].ConnectionString);
            var _dt = new DataTable();

            // --> tous les docs liés au contrat
            var sql = @"SELECT * FROM (	
                                   SELECT   m.pk as mediaPk, td.id as catPk, td.RANK AS CatRank,m.nom as mediaNom, td.nom as catNom, tdm.date AS dateDoc, 
                                            typeDoc = 'contrat', c.pk AS Fk, DATEDIFF(year, tdm.date, GETDATE()) as d
                                   FROM	    [Nortiaca_MEDIA].[dbo].[CA_MEDIA] m
                                            INNER JOIN Nortiaca.dbo.ca_contrat c on c.pk = m.pkvalue
                                            INNER JOIN Nortiaca.dbo.ca_typedocs_media as tdm on tdm.fkmedia = m.pk
                                            INNER JOIN Nortiaca.dbo.ca_typedocs as td on td.id = tdm.fktypedocs
                                            INNER JOIN Nortiaca.dbo.PART_AGENCE AS ag ON ag.PART_AGENCE_ID = c.FKAPPORTEUR
                                   WHERE	c.CleSalesForce = @contratCleSf 
					                        AND ag.PART_AGENCE_CleSalesForce = @appCleSf
                                            AND [table] = 'ca_contrat' 
                                            AND (m.VisibleNOL = 1 OR m.VisibleNOL is null) AND (m.Original = 1 or m.Original is null) 
                   
                                   UNION
                   
                                   SELECT	m.pk as mediaPk, td.id as catPk,td.RANK AS CatRank, m.nom as mediaNom, td.nom as catNom, tdm.date AS dateDoc, 
                                            typeDoc = 'sous', sous.pk AS Fk, DATEDIFF(year, tdm.date, GETDATE()) as d
                                   FROM	    [Nortiaca_MEDIA].[dbo].[CA_MEDIA] m
                                            INNER JOIN Nortiaca.dbo.ca_souscripteur sous on sous.pk = m.pkvalue
                                            INNER JOIN Nortiaca.dbo.ca_contrat c on c.fksouscripteur = m.pkvalue
                                            INNER JOIN Nortiaca.dbo.ca_typedocs_media as tdm on tdm.fkmedia = m.pk
                                            INNER JOIN Nortiaca.dbo.ca_typedocs as td on td.id = tdm.fktypedocs  
                                            INNER JOIN Nortiaca.dbo.PART_AGENCE AS ag ON ag.PART_AGENCE_ID = c.FKAPPORTEUR
                                   WHERE    c.CleSalesForce = @contratCleSf 
					                        AND ag.PART_AGENCE_CleSalesForce = @appCleSf
                                            AND [table] = 'ca_souscripteur' 
                                            AND (m.VisibleNOL = 1 OR m.VisibleNOL is null) AND (m.Original = 1 or m.Original is null) 
                   
                              ) AS t  
                        WHERE	(catPk not in (" + excludedCat + ")) AND (catPk in (" + catPkContrats + "," + catPkSous +  ") or CatRank in (" + catPkContrats + "," + catPkSous + ")) " +
                       "ORDER BY typeDoc, Fk, dateDoc desc ";

            var cmd = new SqlCommand(sql, _ocon);
            cmd.Parameters.Add("@appCleSf", SqlDbType.NVarChar).Value = appCleSf;
            cmd.Parameters.Add("@contratCleSf", SqlDbType.NVarChar).Value = contratCleSf;
            var da = new SqlDataAdapter(cmd);
            da.Fill(_dt);
            _ocon.Close();

            return _dt;
        }

        public static DataTable GetDocumentMvt(string appCleSf, string contratCleSf, string catPkAllMvts, string excludedCat)
        {
            var _ocon = new SqlConnection(ConfigurationManager.ConnectionStrings["dsnNortiaCA"].ConnectionString);
            var _dt = new DataTable();

            // --> mouvements 
            var sql = @"SELECT * FROM (	
                               SELECT	m.pk as mediaPk, td.id as catPk,td.RANK AS CatRank, m.nom as mediaNom, td.nom as catNom, tdm.date AS dateDoc, 
                                        typeDoc = 'Mouvement', mvt.pk AS Fk,
                                        mvt.date AS dateMvt, mvt.MONTANT, mvt.ReferenceInterne as ref, DATEDIFF(year, tdm.date, GETDATE()) as d
                               FROM	    [Nortiaca_MEDIA].[dbo].[CA_MEDIA] m
                                        INNER JOIN Nortiaca.dbo.CA_MOUVEMENT mvt on mvt.pk = m.pkvalue
                                        INNER JOIN Nortiaca.dbo.CA_CONTRAT c ON mvt.fkcontrat = c.PK
                                        INNER JOIN Nortiaca.dbo.ca_typedocs_media as tdm on tdm.fkmedia = m.pk
                                        INNER JOIN Nortiaca.dbo.ca_typedocs as td on td.id = tdm.fktypedocs
                                        INNER JOIN Nortiaca.dbo.PART_AGENCE AS ag ON ag.PART_AGENCE_ID = c.FKAPPORTEUR
                               WHERE	c.CleSalesForce = @contratCleSf 
			                            AND ag.PART_AGENCE_CleSalesForce = @appCleSf 
                                        AND [table] = 'CA_MOUVEMENT' 
                                        AND (m.VisibleNOL = 1 OR m.VisibleNOL is null) AND (m.Original = 1 or m.Original is null)

                               UNION

                               SELECT	m.pk as mediaPk, td.id as catPk,td.RANK AS CatRank, m.nom as mediaNom, td.nom as catNom, tdm.date AS dateDoc, 
                                        typeDoc = 'Mouvement programmé', mvt.pk AS Fk,
                                        mvt.datecreation AS dateMvt, mvt.montant, mvt.ReferenceInterne as ref, DATEDIFF(year, tdm.date, GETDATE()) as d
                               FROM	    [Nortiaca_MEDIA].[dbo].[CA_MEDIA] m
                                        INNER JOIN Nortiaca.dbo.ca_mvt_prog mvt on mvt.pk = m.pkvalue
                                        INNER JOIN Nortiaca.dbo.CA_CONTRAT c ON mvt.fkcontrat = c.PK
                                        INNER JOIN Nortiaca.dbo.ca_typedocs_media as tdm on tdm.fkmedia = m.pk
                                        INNER JOIN Nortiaca.dbo.ca_typedocs as td on td.id = tdm.fktypedocs  
                                        INNER JOIN Nortiaca.dbo.PART_AGENCE AS ag ON ag.PART_AGENCE_ID = c.FKAPPORTEUR
                              WHERE	    c.CleSalesForce = @contratCleSf 
			                            AND ag.PART_AGENCE_CleSalesForce = @appCleSf 
                                        AND [table] = 'ca_mvt_prog' 
                                        AND (m.VisibleNOL = 1 OR m.VisibleNOL is null)  AND (m.Original = 1 or m.Original is null) 

                               UNION

                               SELECT	m.pk as mediaPk, td.id as catPk, td.RANK AS CatRank,m.nom as mediaNom, td.nom as catNom, tdm.date AS dateDoc, 
                                        typeDoc = 'Arbitrage', mvt.id AS Fk,
                                        mvt.DATECREATION AS dateMvt, mvt.MONTANT, mvt.ReferenceInterne as ref, DATEDIFF(year, tdm.date, GETDATE()) as d
                               FROM	    [Nortiaca_MEDIA].[dbo].[CA_MEDIA] m
                                        INNER JOIN Nortiaca.dbo.ca_arbitrage mvt on mvt.id = m.pkvalue
                                        INNER JOIN Nortiaca.dbo.CA_CONTRAT c ON mvt.fkcontrat = c.PK
                                        INNER JOIN Nortiaca.dbo.ca_typedocs_media as tdm on tdm.fkmedia = m.pk
                                        INNER JOIN Nortiaca.dbo.ca_typedocs as td on td.id = tdm.fktypedocs 
                                        INNER JOIN Nortiaca.dbo.PART_AGENCE AS ag ON ag.PART_AGENCE_ID = c.FKAPPORTEUR
                               WHERE	c.CleSalesForce = @contratCleSf 
			                            AND ag.PART_AGENCE_CleSalesForce = @appCleSf 
                                        AND [table] = 'ca_arbitrage' 
                                        AND (m.VisibleNOL = 1 OR m.VisibleNOL is null) AND (m.Original = 1 or m.Original is null)

                            ) AS t  
              WHERE	(catPk not in (" + excludedCat + ")) AND (catPk in (" + catPkAllMvts + ") or CatRank in (" + catPkAllMvts + "))   " +
              "ORDER BY typeDoc, Fk, dateDoc desc";

            var cmd = new SqlCommand(sql, _ocon);
            cmd.Parameters.Add("@appCleSf", SqlDbType.NVarChar).Value = appCleSf;
            cmd.Parameters.Add("@contratCleSf", SqlDbType.NVarChar).Value = contratCleSf;
            var da = new SqlDataAdapter(cmd);
            da.Fill(_dt);
            _ocon.Close();

            return _dt;
        }
    }

    
}
