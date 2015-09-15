using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using GeniusX.AXA.CustomUI.Resources;

namespace GeniusX.AXA.CustomUI
{
    public partial class PolicySummary : System.Web.UI.Page
    {
        private const string PRODUCT_GBIPC = "GBIPC";
        private const string PRODUCT_GBIMO = "GBIMO";
        private const string ERROR_MESSAGE = "Some error has occured"; 
        private const string UW_POLICYSUMMARY_PROCEDURE = "[Uw].[AXAPolicySummary]";

        // XML Template for MOTOR
        private string motorTemplate = @"<?xml version=""1.0""?>
                <?mso-application progid=""Excel.Sheet""?>
                <Workbook xmlns=""urn:schemas-microsoft-com:office:spreadsheet""
                    xmlns:o=""urn:schemas-microsoft-com:office:office""
                    xmlns:x=""urn:schemas-microsoft-com:office:excel""
                    xmlns:ss=""urn:schemas-microsoft-com:office:spreadsheet""
                    xmlns:html=""http://www.w3.org/TR/REC-html40"">
                    <DocumentProperties xmlns=""urn:schemas-microsoft-com:office:office"">
                        <Author>Genius.X</Author>
                    </DocumentProperties>
                    <ExcelWorkbook xmlns=""urn:schemas-microsoft-com:office:excel""/>
                    <Styles>
                    <Style ss:ID=""Default"" ss:Name=""Normal"">
                    <Alignment ss:Vertical=""Bottom""/>
                    <Borders/>
                    <Font ss:FontName=""Calibri"" x:Family=""Swiss"" ss:Size=""11"" ss:Color=""#000000""/>
                    <Interior/>
                    <NumberFormat/>
                    <Protection/>
                    </Style>
                    <Style ss:ID=""s62"">
                    <Alignment ss:Horizontal=""Center"" ss:Vertical=""Bottom"" ss:WrapText=""1""/>
                    <Borders>
                    <Border ss:Position=""Bottom"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Left"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Right"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Top"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    </Borders>
                    <Font ss:FontName=""Calibri"" x:Family=""Swiss"" ss:Size=""11"" ss:Color=""#000000"" ss:Bold=""1""/>
                    <Interior ss:Color=""#FFFF00"" ss:Pattern=""Solid""/>
                    </Style>
                    <Style ss:ID=""s63"">
                    <Alignment ss:Horizontal=""Center"" ss:Vertical=""Bottom""/>
                    </Style>
                    <Style ss:ID=""s64"">
                    <Alignment ss:Horizontal=""Center"" ss:Vertical=""Bottom""/>
                    <Borders>
                    <Border ss:Position=""Bottom"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Left"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Right"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Top"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    </Borders>
                    </Style>
                    <Style ss:ID=""s65"">
                    <Alignment ss:Horizontal=""Left"" ss:Vertical=""Bottom""/>
                    <Borders>
                    <Border ss:Position=""Bottom"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Left"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Right"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Top"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    </Borders>
                    </Style>
                    <Style ss:ID=""s66"">
                    <Alignment ss:Horizontal=""Left"" ss:Vertical=""Bottom""/>
                    <Borders>
                    <Border ss:Position=""Bottom"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Left"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Right"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Top"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    </Borders>
                    <Interior/>
                    </Style>
                    <Style ss:ID=""s67"">
                    <Alignment ss:Horizontal=""Left"" ss:Vertical=""Bottom"" ss:WrapText=""1""/>
                    <Borders>
                    <Border ss:Position=""Bottom"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Left"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Right"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Top"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    </Borders>
                    </Style>
                    <Style ss:ID=""s68"">
                    <Alignment ss:Horizontal=""Center"" ss:Vertical=""Bottom""/>
                    <Borders>
                    <Border ss:Position=""Bottom"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Left"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Right"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Top"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    </Borders>
                    <NumberFormat ss:Format=""Fixed""/>
                    </Style>
                    <Style ss:ID=""s69"">
                    <Alignment ss:Horizontal=""Left"" ss:Vertical=""Bottom""/>
                    <Borders>
                    <Border ss:Position=""Bottom"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Left"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Right"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Top"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    </Borders>
                    <Font ss:FontName=""Wingdings"" x:CharSet=""2"" ss:Size=""11"" ss:Color=""#000000""/>
                    </Style>
                    <Style ss:ID=""s70"">
                    <Alignment ss:Horizontal=""Left"" ss:Vertical=""Top"" ss:WrapText=""1""/>
                    <Borders>
                    <Border ss:Position=""Bottom"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Left"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Right"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Top"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    </Borders>
                    <Font ss:FontName=""Calibri"" x:Family=""Swiss"" ss:Size=""11"" ss:Color=""#000000"" ss:Bold=""1""/>
                    <Interior ss:Color=""#FFFF00"" ss:Pattern=""Solid""/>
                    </Style>
                    <Style ss:ID=""s71"">
                    <Alignment ss:Horizontal=""Center"" ss:Vertical=""Top"" ss:WrapText=""1""/>
                    <Borders>
                    <Border ss:Position=""Bottom"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Left"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Right"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Top"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    </Borders>
                    <Font ss:FontName=""Calibri"" x:Family=""Swiss"" ss:Size=""11"" ss:Color=""#000000"" ss:Bold=""1""/>
                    <Interior ss:Color=""#FFFF00"" ss:Pattern=""Solid""/>
                    </Style>
                    </Styles>
                    <Worksheet ss:Name=""Summary"">
                        <Table ss:StyleID=""s63"" ss:DefaultRowHeight=""15"">
                        <Column ss:StyleID=""s63"" ss:AutoFitWidth=""0"" ss:Width=""119.25""/>
                        <Column ss:StyleID=""s63"" ss:AutoFitWidth=""0"" ss:Width=""100.5""/>
                        <Column ss:StyleID=""s63"" ss:AutoFitWidth=""0"" ss:Width=""187.5""/>
                        <Column ss:StyleID=""s63"" ss:AutoFitWidth=""0"" ss:Width=""111.75""/>
                        <Column ss:StyleID=""s63"" ss:AutoFitWidth=""0"" ss:Width=""64.5""/>
                        <Column ss:StyleID=""s63"" ss:AutoFitWidth=""0"" ss:Width=""201""/>
                        <Column ss:StyleID=""s63"" ss:Width=""114.75""/>
                        <Column ss:StyleID=""s63"" ss:Width=""56.25"" ss:Span=""1""/>
                        <Column ss:Index=""10"" ss:StyleID=""s63"" ss:Width=""95.25""/>
                        <Column ss:StyleID=""s63"" ss:Width=""105.75""/>
                        <Column ss:StyleID=""s63"" ss:Width=""99.75""/>
                        <Column ss:StyleID=""s63"" ss:Width=""93""/>
                        <Column ss:StyleID=""s63"" ss:AutoFitWidth=""0"" ss:Width=""80.25""/>
                        <Column ss:StyleID=""s63"" ss:Width=""60.75""/>
                        <Column ss:StyleID=""s63"" ss:Width=""56.25"" ss:Span=""1""/>
                        <Column ss:Index=""18"" ss:StyleID=""s63"" ss:AutoFitWidth=""0"" ss:Width=""58.5""/>
                        <Column ss:StyleID=""s63"" ss:Width=""51"" ss:Span=""2""/>
                        <Column ss:Index=""22"" ss:StyleID=""s63"" ss:Width=""54""/>
                        <Column ss:StyleID=""s63"" ss:Width=""60.75""/>
                        <Column ss:StyleID=""s63"" ss:Width=""51""/>
                        <Row ss:AutoFitHeight=""0"" ss:Height=""42"">[headers]</Row>       
                        [data]
                        </Table>
                    <WorksheetOptions xmlns=""urn:schemas-microsoft-com:office:excel"">
                    <PageSetup>
                    <Header x:Margin=""0.3""/>
                    <Footer x:Margin=""0.3""/>
                    <PageMargins x:Bottom=""0.75"" x:Left=""0.7"" x:Right=""0.7"" x:Top=""0.75""/>
                    </PageSetup>
                    <Panes>
                    <Pane>
                        <Number>3</Number>
                        <ActiveRow>6</ActiveRow>
                        <ActiveCol>5</ActiveCol>
                    </Pane>
                    </Panes>
                    <ProtectObjects>False</ProtectObjects>
                    <ProtectScenarios>False</ProtectScenarios>
                    </WorksheetOptions>
                    </Worksheet>
                </Workbook>
                ";


        // XML Template for Liability
        private string liabilityTemplate = @"<?xml version=""1.0""?>
                <?mso-application progid=""Excel.Sheet""?>
                <Workbook xmlns=""urn:schemas-microsoft-com:office:spreadsheet""
                 xmlns:o=""urn:schemas-microsoft-com:office:office""
                 xmlns:x=""urn:schemas-microsoft-com:office:excel""
                 xmlns:ss=""urn:schemas-microsoft-com:office:spreadsheet""
                 xmlns:html=""http://www.w3.org/TR/REC-html40"">
                 <DocumentProperties xmlns=""urn:schemas-microsoft-com:office:office"">
                    <Author>Genius.X</Author>
                 </DocumentProperties>
                 <ExcelWorkbook xmlns=""urn:schemas-microsoft-com:office:excel""/>
                 <Styles>
                  <Style ss:ID=""Default"" ss:Name=""Normal"">
                   <Alignment ss:Vertical=""Bottom""/>
                   <Borders/>
                   <Font ss:FontName=""Calibri"" x:Family=""Swiss"" ss:Size=""11"" ss:Color=""#000000""/>
                   <Interior/>
                   <NumberFormat/>
                   <Protection/>
                  </Style>
                  <Style ss:ID=""s62"">
                   <Alignment ss:Horizontal=""Center"" ss:Vertical=""Bottom""/>
                  </Style>
                  <Style ss:ID=""s63"">
                   <Alignment ss:Horizontal=""Center"" ss:Vertical=""Bottom""/>
                   <Borders>
                    <Border ss:Position=""Bottom"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Left"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Right"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Top"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                   </Borders>
                   <Interior/>
                  </Style>
                  <Style ss:ID=""s64"">
                   <Alignment ss:Horizontal=""Center"" ss:Vertical=""Bottom""/>
                   <Interior/>
                  </Style>
                  <Style ss:ID=""s65"">
                   <Alignment ss:Horizontal=""Left"" ss:Vertical=""Bottom""/>
                   <Borders>
                    <Border ss:Position=""Bottom"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Left"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Right"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Top"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                   </Borders>
                  </Style>
                  <Style ss:ID=""s66"">
                   <Alignment ss:Horizontal=""Left"" ss:Vertical=""Bottom""/>
                   <Borders>
                    <Border ss:Position=""Bottom"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Left"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Right"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Top"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                   </Borders>
                   <Interior/>
                  </Style>
                  <Style ss:ID=""s67"">
                   <Alignment ss:Horizontal=""Left"" ss:Vertical=""Bottom"" ss:WrapText=""1""/>
                   <Borders>
                    <Border ss:Position=""Bottom"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Left"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Right"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Top"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                   </Borders>
                   <Interior/>
                  </Style>
                  <Style ss:ID=""s68"">
                   <Alignment ss:Horizontal=""Center"" ss:Vertical=""Bottom""/>
                   <Borders>
                    <Border ss:Position=""Bottom"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Left"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Right"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Top"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                   </Borders>
                   <Interior/>
                   <NumberFormat ss:Format=""Fixed""/>
                  </Style>
                  <Style ss:ID=""s69"">
                   <Alignment ss:Horizontal=""Left"" ss:Vertical=""Top"" ss:WrapText=""1""/>
                   <Borders>
                    <Border ss:Position=""Bottom"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Left"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Right"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Top"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                   </Borders>
                   <Font ss:FontName=""Calibri"" x:Family=""Swiss"" ss:Size=""11"" ss:Color=""#000000"" ss:Bold=""1""/>
                   <Interior ss:Color=""#FFFF00"" ss:Pattern=""Solid""/>
                  </Style>
                  <Style ss:ID=""s70"">
                   <Alignment ss:Horizontal=""Center"" ss:Vertical=""Top"" ss:WrapText=""1""/>
                   <Borders>
                    <Border ss:Position=""Bottom"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Left"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Right"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                    <Border ss:Position=""Top"" ss:LineStyle=""Continuous"" ss:Weight=""1""/>
                   </Borders>
                   <Font ss:FontName=""Calibri"" x:Family=""Swiss"" ss:Size=""11"" ss:Color=""#000000"" ss:Bold=""1""/>
                   <Interior ss:Color=""#FFFF00"" ss:Pattern=""Solid""/>
                  </Style>
                  <Style ss:ID=""s71"">
                   <Alignment ss:Horizontal=""Center"" ss:Vertical=""Top""/>
                  </Style>
                 </Styles>
                 <Worksheet ss:Name=""Summary"">
                  <Table ss:StyleID=""s62"" ss:DefaultRowHeight=""15"">
                   <Column ss:StyleID=""s62"" ss:AutoFitWidth=""0"" ss:Width=""111""/>
                   <Column ss:StyleID=""s62"" ss:AutoFitWidth=""0"" ss:Width=""89.25""/>
                   <Column ss:StyleID=""s62"" ss:AutoFitWidth=""0"" ss:Width=""156""/>
                   <Column ss:StyleID=""s62"" ss:AutoFitWidth=""0"" ss:Width=""111.75""/>
                   <Column ss:StyleID=""s62"" ss:AutoFitWidth=""0"" ss:Width=""64.5""/>
                   <Column ss:StyleID=""s62"" ss:AutoFitWidth=""0"" ss:Width=""182.25""/>
                   <Column ss:StyleID=""s62"" ss:Width=""114.75""/>
                   <Column ss:StyleID=""s62"" ss:Width=""56.25"" ss:Span=""1""/>
                   <Column ss:Index=""10"" ss:StyleID=""s62"" ss:Width=""95.25""/>
                   <Column ss:StyleID=""s62"" ss:Width=""65.25""/>
                   <Column ss:StyleID=""s62"" ss:Width=""40.5""/>
                   <Column ss:StyleID=""s62"" ss:Width=""93""/>
                   <Column ss:StyleID=""s62"" ss:AutoFitWidth=""0"" ss:Width=""80.25""/>
                   <Column ss:StyleID=""s62"" ss:Width=""60.75""/>
                   <Column ss:StyleID=""s62"" ss:Width=""56.25"" ss:Span=""1""/>
                   <Row ss:AutoFitHeight=""0"" ss:Height=""43.875"" ss:StyleID=""s71"">[headers]</Row>
                   [data]
                  </Table>
                  <WorksheetOptions xmlns=""urn:schemas-microsoft-com:office:excel"">
                   <PageSetup>
                    <Header x:Margin=""0.3""/>
                    <Footer x:Margin=""0.3""/>
                    <PageMargins x:Bottom=""0.75"" x:Left=""0.7"" x:Right=""0.7"" x:Top=""0.75""/>
                   </PageSetup>
                   <Selected/>
                   <Panes>
                    <Pane>
                     <Number>3</Number>
                     <ActiveCol>1</ActiveCol>
                    </Pane>
                   </Panes>
                   <ProtectObjects>False</ProtectObjects>
                   <ProtectScenarios>False</ProtectScenarios>
                  </WorksheetOptions>
                 </Worksheet>
                </Workbook>
                ";

        protected void Page_Load(object sender, EventArgs e)
        {
            string filename = Request.QueryString["Filename"];
            long retHeaderID;
            bool headerIdflag = long.TryParse(Request.QueryString["PolicyHeaderId"], out retHeaderID);
            if (!headerIdflag)
            {
                // Error message displayed if the headerId is incorrect
                System.Web.HttpContext.Current.Response.Write(@"<script language='javascript'>alert('" + ERROR_MESSAGE + "');</script>");
            }
            else
            {
                string policyHeaderId = Request.QueryString["PolicyHeaderId"];
                string productType = Request.QueryString["ProductType"];
                string policyHeaderReference = Request.QueryString["HeaderReference"];

                string excelXML = this.GetDataSheetAsString(policyHeaderId, productType, policyHeaderReference);
                byte[] buffer = Encoding.ASCII.GetBytes(excelXML);
                Response.Clear();
                Response.AddHeader("Content-Disposition", "attachment;filename=" + filename + ".xls");
                Response.ContentType = "application/vnd.ms-excel";  ////application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
                Response.BinaryWrite(buffer);
                Response.End();
            }
        }

        private string GetDataSheetAsString(string policyHeaderId, string productType, string policyHeaderReference)
        {
            string selectedTemplate = null;
            if (PRODUCT_GBIPC.Equals(productType))
            {
                selectedTemplate = this.liabilityTemplate;
            }
            else
            {
                selectedTemplate = this.motorTemplate;
            }

            string headers = CreateHeader(productType, policyHeaderReference);

            string data = GetPolicyDataAsString(policyHeaderId, productType);

            string excelXML = selectedTemplate.Replace("[headers]", headers).Replace("[data]", data);
            return excelXML;
        }

        // This method generates column headers for the datasheet
        private static string CreateHeader(string productType, string policyHeaderReference)
        {
            string headers = string.Empty;
            string levelheader = "Level &#10;(in " + policyHeaderReference + ")";
            if (PRODUCT_GBIPC.Equals(productType))
            {
                headers += "<Cell ss:StyleID=\"s69\"><Data ss:Type=\"String\">" + levelheader + "</Data></Cell>\n";
                headers += "<Cell ss:StyleID=\"s69\"><Data ss:Type=\"String\">Section Title</Data></Cell>\n";
                headers += "<Cell ss:StyleID=\"s69\"><Data ss:Type=\"String\">Section Detail Title</Data></Cell>\n";
                headers += "<Cell ss:StyleID=\"s69\"><Data ss:Type=\"String\">Coverage Title</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s69\"><Data ss:Type=\"String\">Type</Data></Cell>\n";
                headers += "<Cell ss:StyleID=\"s69\"><Data ss:Type=\"String\">Description</Data></Cell>\n";
                headers += "<Cell ss:StyleID=\"s70\"><Data ss:Type=\"String\">Aggregate / Deductible &#10;Amount</Data></Cell>\n";
                headers += "<Cell ss:StyleID=\"s70\"><Data ss:Type=\"String\">Deductible &#10;Sequence</Data></Cell>\n";
                headers += "<Cell ss:StyleID=\"s69\"><Data ss:Type=\"String\">Deductible &#10;Type</Data></Cell>\n";
                headers += "<Cell ss:StyleID=\"s69\"><Data ss:Type=\"String\">Policy &#10;Reference</Data></Cell>\n";
                headers += "<Cell ss:StyleID=\"s69\"><Data ss:Type=\"String\">Division</Data></Cell>\n";
                headers += "<Cell ss:StyleID=\"s69\"><Data ss:Type=\"String\">Vehicle &#10;Type</Data></Cell>\n";
                headers += "<Cell ss:StyleID=\"s69\"><Data ss:Type=\"String\">Deductible &#10;Reason</Data></Cell>\n";
                headers += "<Cell ss:StyleID=\"s69\"><Data ss:Type=\"String\">Aggregate Adjustable</Data></Cell>\n";
                headers += "<Cell ss:StyleID=\"s69\"><Data ss:Type=\"String\">Deductible Adjustable?</Data></Cell>\n";
                headers += "<Cell ss:StyleID=\"s69\"><Data ss:Type=\"String\">Funded?</Data></Cell>\n";
                headers += "<Cell ss:StyleID=\"s69\"><Data ss:Type=\"String\">Ranking?</Data></Cell>\n";
            }
            else
            {
                headers += "<Cell ss:StyleID=\"s70\"><Data ss:Type=\"String\">" + levelheader + "</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s70\"><Data ss:Type=\"String\">Section &#10;Title</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s70\"><Data ss:Type=\"String\">Section Detail Title</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s70\"><Data ss:Type=\"String\">Coverage Title</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s70\"><Data ss:Type=\"String\">Type</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s70\"><Data ss:Type=\"String\">Description</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s71\"><Data ss:Type=\"String\">Aggregate / Deductible &#10;Amount</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s71\"><Data ss:Type=\"String\">Deductible &#10;Sequence</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s70\"><Data ss:Type=\"String\">Deductible &#10;Type</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s70\"><Data ss:Type=\"String\">Policy &#10;Reference</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s70\"><Data ss:Type=\"String\">Division</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s70\"><Data ss:Type=\"String\">Vehicle &#10;Type</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s70\"><Data ss:Type=\"String\">Deductible &#10;Reason</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s70\"><Data ss:Type=\"String\">Aggregate Adjustable</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s70\"><Data ss:Type=\"String\">Deductible Adjustable?</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s70\"><Data ss:Type=\"String\">Funded?</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s70\"><Data ss:Type=\"String\">&#10;Ranking?</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s71\"><Data ss:Type=\"String\">AD Excess Amount - Accident</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s71\"><Data ss:Type=\"String\">AD Excess Amount - Fire</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s71\"><Data ss:Type=\"String\">AD Excess Amount - Theft</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s71\"><Data ss:Type=\"String\">AD Excess Amount - Storm</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s71\"><Data ss:Type=\"String\">AD Excess Amount - Vandalism</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s62\"><Data ss:Type=\"String\">AD Excess Amount - Windscreen Only</Data></Cell>";
                headers += "<Cell ss:StyleID=\"s62\"><Data ss:Type=\"String\">AD Excess Amount - Other</Data></Cell>";
            }
            return headers;
        }

        // This method generates datarows for the datasheet
        private static string GetPolicyDataAsString(string policyHeaderId, string productType)
        {
            string data = string.Empty;
            using (SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["Config"].ConnectionString))
            {
                using (SqlCommand sqlCommand = new SqlCommand())
                {
                    sqlConnection.Open();
                    sqlCommand.Connection = sqlConnection;
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.CommandText = UW_POLICYSUMMARY_PROCEDURE;
                    sqlCommand.Parameters.AddWithValue("@headerID", policyHeaderId);

                    try
                    {
                        sqlCommand.ExecuteNonQuery();

                        using (SqlDataReader dataReader = sqlCommand.ExecuteReader())
                        {
                            if (dataReader != null && dataReader.HasRows)
                            {
                                while (dataReader.Read())
                                {
                                    data += "<Row>\n";

                                    if (dataReader["SystemComponent"] != DBNull.Value)
                                    {
                                        data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + dataReader["SystemComponent"].ToString() + "</Data></Cell>\n";
                                    }
                                    else
                                    {
                                        data += "<Cell ss:StyleID=\"s65\"/>\n";
                                    }

                                    if (dataReader["SectionTitle"] != DBNull.Value)
                                    {
                                        data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + dataReader["SectionTitle"].ToString() + "</Data></Cell>\n";
                                    }
                                    else
                                    {
                                        data += "<Cell ss:StyleID=\"s65\"/>\n";
                                    }

                                    if (dataReader["SectionDetailTitle"] != DBNull.Value)
                                    {
                                        data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + dataReader["SectionDetailTitle"].ToString() + "</Data></Cell>\n";
                                    }
                                    else
                                    {
                                        data += "<Cell ss:StyleID=\"s65\"/>\n";
                                    }

                                    if (dataReader["CoverageTitle"] != DBNull.Value)
                                    {
                                        data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + dataReader["CoverageTitle"].ToString() + "</Data></Cell>\n";
                                    }
                                    else
                                    {
                                        data += "<Cell ss:StyleID=\"s65\"/>\n";
                                    }

                                    if (dataReader["Type"] != DBNull.Value)
                                    {
                                        data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + dataReader["Type"].ToString() + "</Data></Cell>\n";
                                    }
                                    else
                                    {
                                        data += "<Cell ss:StyleID=\"s65\"/>\n";
                                    }

                                    if (dataReader["Description"] != DBNull.Value)
                                    {
                                        data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + dataReader["Description"].ToString() + "</Data></Cell>\n";
                                    }
                                    else
                                    {
                                        data += "<Cell ss:StyleID=\"s65\"/>\n";
                                    }

                                    if (dataReader["AggregateDeductibleAmount"] != DBNull.Value)
                                    {
                                        data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + DecimalConverter(dataReader["AggregateDeductibleAmount"].ToString(),2) + "</Data></Cell>\n";
                                    }
                                    else
                                    {
                                        data += "<Cell ss:StyleID=\"s65\"/>\n";
                                    }

                                    if (dataReader["DeductibleSequence"] != DBNull.Value)
                                    {
                                        data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + DecimalConverter(dataReader["DeductibleSequence"].ToString(),0) + "</Data></Cell>\n";
                                    }
                                    else
                                    {
                                        data += "<Cell ss:StyleID=\"s65\"/>\n";
                                    }

                                    if (dataReader["DeductibleType"] != DBNull.Value)
                                    {
                                        data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + dataReader["DeductibleType"].ToString() + "</Data></Cell>\n";
                                    }
                                    else
                                    {
                                        data += "<Cell ss:StyleID=\"s65\"/>\n";
                                    }

                                    if (dataReader["PolicyReference"] != DBNull.Value)
                                    {
                                        data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + dataReader["PolicyReference"].ToString() + "</Data></Cell>\n";
                                    }
                                    else
                                    {
                                        data += "<Cell ss:StyleID=\"s65\"/>\n";
                                    }

                                    if (dataReader["Division"] != DBNull.Value)
                                    {
                                        data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + dataReader["Division"].ToString() + "</Data></Cell>\n";
                                    }
                                    else
                                    {
                                        data += "<Cell ss:StyleID=\"s65\"/>\n";
                                    }

                                    if (dataReader["VehicleType"] != DBNull.Value)
                                    {
                                        data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + dataReader["VehicleType"].ToString() + "</Data></Cell>\n";
                                    }
                                    else
                                    {
                                        data += "<Cell ss:StyleID=\"s65\"/>\n";
                                    }

                                    if (dataReader["DeductibleReason"] != DBNull.Value)
                                    {
                                        data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + dataReader["DeductibleReason"].ToString() + "</Data></Cell>\n";
                                    }
                                    else
                                    {
                                        data += "<Cell ss:StyleID=\"s65\"/>\n";
                                    }

                                    if (dataReader["AggregateAdjustable"] != DBNull.Value)
                                    {
                                        data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + dataReader["AggregateAdjustable"].ToString() + "</Data></Cell>\n";
                                    }
                                    else
                                    {
                                        data += "<Cell ss:StyleID=\"s65\"/>\n";
                                    }

                                    if (dataReader["DeductibleAdjustable"] != DBNull.Value)
                                    {
                                        if (String.Equals(dataReader["DeductibleAdjustable"].ToString(), StringResources.TRUE))
                                        {
                                            data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + StringResources.CONVERT_TRUE_YES + "</Data></Cell>\n";
                                        }
                                        else
                                        {
                                            data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + StringResources.CONVERT_FALSE_NO + "</Data></Cell>\n";
                                        }
                                    }
                                    else
                                    {
                                        data += "<Cell ss:StyleID=\"s65\"/>\n";
                                    }

                                    if (dataReader["Funded"] != DBNull.Value)
                                    {
                                        if (String.Equals(dataReader["Funded"].ToString(), StringResources.TRUE))
                                        {
                                            data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + StringResources.CONVERT_TRUE_YES + "</Data></Cell>\n";
                                        }
                                        else
                                        {
                                            data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + StringResources.CONVERT_FALSE_NO + "</Data></Cell>\n";
                                        }
                                    }
                                    else
                                    {
                                        data += "<Cell ss:StyleID=\"s65\"/>\n";
                                    }

                                    if (dataReader["Ranking"] != DBNull.Value)
                                    {
                                        if (String.Equals(dataReader["Ranking"].ToString(), StringResources.TRUE))
                                        {
                                            data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + StringResources.CONVERT_TRUE_YES + "</Data></Cell>\n";
                                        }
                                        else
                                        {
                                            data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + StringResources.CONVERT_FALSE_NO + "</Data></Cell>\n";
                                        }
                                    }
                                    else
                                    {
                                        data += "<Cell ss:StyleID=\"s65\"/>\n";
                                    }

                                    if (PRODUCT_GBIMO.Equals(productType))
                                    {
                                        if (dataReader["ADExcessAmountAccident"] != DBNull.Value)
                                        {
                                            data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + DecimalConverter(dataReader["ADExcessAmountAccident"].ToString(),2) + "</Data></Cell>\n";
                                        }
                                        else
                                        {
                                            data += "<Cell ss:StyleID=\"s65\"/>\n";
                                        }

                                        if (dataReader["ADExcessAmountFire"] != DBNull.Value)
                                        {
                                            data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + DecimalConverter(dataReader["ADExcessAmountFire"].ToString(),2) + "</Data></Cell>\n";
                                        }
                                        else
                                        {
                                            data += "<Cell ss:StyleID=\"s65\"/>\n";
                                        }

                                        if (dataReader["ADExcessAmountTheft"] != DBNull.Value)
                                        {
                                            data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + DecimalConverter(dataReader["ADExcessAmountTheft"].ToString(),2) + "</Data></Cell>\n";
                                        }
                                        else
                                        {
                                            data += "<Cell ss:StyleID=\"s65\"/>\n";
                                        }

                                        if (dataReader["ADExcessAmountStorm"] != DBNull.Value)
                                        {
                                            data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + DecimalConverter(dataReader["ADExcessAmountStorm"].ToString(),2) + "</Data></Cell>\n";
                                        }
                                        else
                                        {
                                            data += "<Cell ss:StyleID=\"s65\"/>\n";
                                        }

                                        if (dataReader["ADExcessAmountVandalism"] != DBNull.Value)
                                        {
                                            data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + DecimalConverter(dataReader["ADExcessAmountVandalism"].ToString(),2) + "</Data></Cell>\n";
                                        }
                                        else
                                        {
                                            data += "<Cell ss:StyleID=\"s65\"/>\n";
                                        }

                                        if (dataReader["ADExcessAmountWindscreen"] != DBNull.Value)
                                        {
                                            data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + DecimalConverter(dataReader["ADExcessAmountWindscreen"].ToString(),2) + "</Data></Cell>\n";
                                        }
                                        else
                                        {
                                            data += "<Cell ss:StyleID=\"s65\"/>\n";
                                        }

                                        if (dataReader["ADExcessAmountOther"] != DBNull.Value)
                                        {
                                            data += "<Cell ss:StyleID=\"s67\"><Data ss:Type=\"String\">" + DecimalConverter(dataReader["ADExcessAmountOther"].ToString(),2) + "</Data></Cell>\n";
                                        }
                                        else
                                        {
                                            data += "<Cell ss:StyleID=\"s65\"/>\n";
                                        }
                                    }

                                    data += "</Row>\n";
                                }
                            }
                        }
                    }

                    finally
                    {

                        sqlConnection.Close();
                    }
                }
            }

            return data;
        }

        private static string DecimalConverter(string value,int decimalplace)
        {
            return System.Math.Round(decimal.Parse(value), decimalplace).ToString();
        }
    }
}