using System;
using System.Data;
using System.ServiceProcess;
using System.Timers;
using System.IO;
using System.Collections;
using System.Data.SqlClient;
using System.Configuration;
using System.Xml;
using System.Text;

namespace HDF_Service
{
    public partial class HDF_Service : ServiceBase
    {
        Timer tmrReset = new Timer();
        private bool blnInProcess = false;
        private string sQuery = string.Empty;
        private SAPbobsCOM.Company oCompany = null;
        private DataTable oDT_Data = null;
        private SqlDataAdapter oSqlAdap = null;
        private DataSet Ds = null;
        private SqlCommand oCommand = null;
        private SAPbobsCOM.Recordset oRecordSet;

        public HDF_Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                traceService("Started Service :" + DateTime.Now);
                tmrReset.Interval = 30000;
                tmrReset.Enabled = true;
                tmrReset.Elapsed += new ElapsedEventHandler(tmrReset_Elapsed);
                this.ConnectSapCompany();
            }
            catch (Exception ex)
            {
                traceService(ex.StackTrace.ToString());
            }
        }

        protected override void OnStop()
        {
            try
            {
                traceService("Stopped Service :" + DateTime.Now);
                disConnectCompany();
                tmrReset.Stop();
            }
            catch (Exception ex)
            {
                traceService(ex.StackTrace.ToString());
            }
        }

        protected override void OnPause()
        {
            try
            {
                traceService("Service Pause :" + DateTime.Now);
                tmrReset.Stop();
            }
            catch (Exception ex)
            {
                traceService(ex.StackTrace.ToString());
            }
        }

        protected override void OnContinue()
        {
            try
            {
                traceService("Service Continues :" + DateTime.Now);
                tmrReset.Start();
            }
            catch (Exception ex)
            {
                traceService(ex.StackTrace.ToString());
            }
        }

        private void tmrReset_Elapsed(object source, ElapsedEventArgs e)
        {
            try
            {
                traceService("Timer Reset At :" + DateTime.Now);
                if (!blnInProcess)
                {
                    ExportLogic();
                }
                else
                {
                    traceService("Still In Process..." + DateTime.Now);
                }
                traceService("Timer Elapses At :" + DateTime.Now);
            }
            catch (Exception ex)
            {
                traceService(ex.StackTrace.ToString());
            }
        }

        private void ExportLogic()
        {
            try
            {
                this.traceService(this.blnInProcess.ToString());
                traceService("Sync Starts...");
               //string strMainDB = System.Configuration.ConfigurationManager.AppSettings["SAPServer"].ToString();

                if (oCompany != null)
                {
                    if (oCompany.Connected)
                    {
                        traceService("Main");
                        export_BasedOnType("GRPO");
                        export_BasedOnType("GR");
                        export_BasedOnType("APCreditMemo");
                        export_BasedOnType("Inventory");
                    }
                }

                blnInProcess = false;
                this.traceService(this.blnInProcess.ToString());
                traceService("Sync Ends...");
            }
            catch (Exception ex)
            {
                traceService(ex.StackTrace.ToString());
                traceService(ex.Message.ToString());
            }
            finally
            {
                blnInProcess = false;
            }
        }

        public void ConnectSapCompany()
        {
            try
            {              

                string strMaiDB = System.Configuration.ConfigurationManager.AppSettings["MainDB"].ToString();
                string DBServer = System.Configuration.ConfigurationManager.AppSettings["SAPServer"].ToString();
                string ServerType = System.Configuration.ConfigurationManager.AppSettings["DbServerType"].ToString();
                string DBUserName = System.Configuration.ConfigurationManager.AppSettings["DbUserName"].ToString();
                string DBPwd = System.Configuration.ConfigurationManager.AppSettings["DbPassword"].ToString();
                string LicenseServer = System.Configuration.ConfigurationManager.AppSettings["SAPlicense"].ToString();
                string strSAPUserName = System.Configuration.ConfigurationManager.AppSettings["SAPUserName"].ToString();
                string strSAPUserPwd = System.Configuration.ConfigurationManager.AppSettings["SAPPassword"].ToString();

                oCompany = new SAPbobsCOM.Company();
                oCompany.Server = DBServer;
                switch (ServerType)
                {
                    case "2008":
                        oCompany.DbServerType = SAPbobsCOM.BoDataServerTypes.dst_MSSQL2008;
                        break;
                    case "2012":
                        oCompany.DbServerType = SAPbobsCOM.BoDataServerTypes.dst_MSSQL2012;
                        break;
                    default:
                        break;
                }
                oCompany.DbUserName = DBUserName;
                oCompany.DbPassword = DBPwd;
                oCompany.CompanyDB = strMaiDB;
                oCompany.UserName = strSAPUserName;
                oCompany.Password = strSAPUserPwd;
                oCompany.UseTrusted = false;

                if (oCompany.Connect() != 0)
                {
                    traceService("Company : " + oCompany.CompanyDB);
                    traceService("Error Code : " + oCompany.GetLastErrorDescription());
                }
                else
                {
                    traceService("Company : " + oCompany.CompanyDB);
                    traceService("Connected");
                }
            }
            catch (Exception ex)
            {
                traceService(ex.StackTrace.ToString());
                traceService(ex.Message.ToString());
                throw;
            }
        }

        private void disConnectCompany()
        {
            try
            {
                if (oCompany != null)
                {
                    if (oCompany.Connected)
                    {
                        oCompany.Disconnect();
                    }
                }
            }
            catch (Exception ex)
            {
                traceService(ex.Message);
            }
        }

        private void traceService(string content)
        {
            try
            {
                string strFile = @"\HDF_Service_" + System.DateTime.Now.ToString("yyyyMMdd") + ".txt";
                string strPath = System.Windows.Forms.Application.StartupPath.ToString() + strFile;
                if (!File.Exists(strPath))
                {
                    File.Create(strPath);
                }
                FileStream fs = new FileStream(strPath, FileMode.Append, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs);
                sw.BaseStream.Seek(0, SeekOrigin.End);
                sw.WriteLine(content);
                sw.Flush();
                sw.Close();
            }
            catch (Exception ex)
            {
                //throw;
            }
        }

        private void export_BasedOnType(string strType)
        {
            try
            {               
                string strMaiDB = System.Configuration.ConfigurationManager.AppSettings["MainDB"].ToString();
                string DBServer = System.Configuration.ConfigurationManager.AppSettings["SAPServer"].ToString();
                string DBUserName = System.Configuration.ConfigurationManager.AppSettings["DbUserName"].ToString();
                string DBPwd = System.Configuration.ConfigurationManager.AppSettings["DbPassword"].ToString();
                object[] args = { DBServer, strMaiDB, DBUserName, DBPwd };
                string strConnection = string.Format(ConfigurationManager.AppSettings["Logger"].ToString(), args);
                traceService(strConnection);
                DataSet oDataSet = null;
                oRecordSet = (SAPbobsCOM.Recordset)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

                if (strType == "GRPO")
                {
                    sQuery = "  Select  T0.DocEntry,T0.DocNum, T0.CardCode ";                    
                    sQuery += " From OPDN T0 Where 1 = 1 AND IsNull(T0.U_Export, 'N')='N' ";
                }
                else if (strType == "GR")
                {
                    sQuery = "Select  T0.DocEntry,T0.DocNum, T0.CardCode ";
                    sQuery += " From ORPD T0 Where 1 = 1 AND IsNull(T0.U_Export, 'N')='N'";
                }
                else if (strType == "APCreditMemo")
                {
                    sQuery = "Select  T0.DocEntry,T0.DocNum, T0.CardCode ";
                    sQuery += " From ORPC T0 Where 1 = 1 AND IsNull(T0.U_Export, 'N')='N'";
                }
                else if (strType == "Inventory")
                {
                    sQuery = "Select  T0.DocEntry,T0.DocNum ";
                    sQuery += " From OIQR T0 Where 1 = 1 AND IsNull(T0.U_Export, 'N')='N'";
                }
                oDT_Data = ExecuteReader(strConnection,sQuery);
                if (oDT_Data != null)
                {
                    if (oDT_Data.Rows.Count > 0)
                    {                       

                        foreach (DataRow dr in oDT_Data.Rows)
                        {
                            Int32 intDocEntry = Convert.ToInt32(dr["DocEntry"].ToString());
                            string strDocNum = dr["DocNum"].ToString();


                            traceService("=========");
                            traceService("Export Type : " + strType);
                            traceService("Export DocNum : " + strDocNum);

                            string EXPath = GetFilePath(strType);
                            string strFileExt = ".xml";
                            string strDFileName = intDocEntry.ToString() + strFileExt;
                            string strDPath = EXPath + "\\" + strDFileName;
                            string strDFNP = strDPath;
                            sQuery = "Exec " + strType + " '" + intDocEntry + "'";
                            oDataSet = ExecuteDataSet(strConnection, sQuery);
                                                     
                            if (oDataSet != null && oDataSet.Tables.Count > 0)
                            {
                                traceService("Record Exists");
                                oDataSet.Tables[0].TableName = "Documents";
                                oDataSet.Tables[1].TableName = "Articles";
                                oDataSet.Tables[2].TableName = "Article";
                                oDataSet.Tables[3].TableName = "Lots";
                                oDataSet.Tables[4].TableName = "Lot";
                            }
                           
                            DataRelation Document = oDataSet.Relations.Add("DocumentDetail", oDataSet.Tables["Documents"].Columns["DocEntry"], oDataSet.Tables["Articles"].Columns["DocEntry"]);
                                                     DataRelation Details = oDataSet.Relations.Add("DDetails", oDataSet.Tables["Articles"].Columns["DocEntry"], oDataSet.Tables["Article"].Columns["DocEntry"]);
                            DataRelation Detail;
                            DataRelation LotsLot;

                            if (strType == "Inventory")
                            {
                                Detail = oDataSet.Relations.Add("DetailsLot", oDataSet.Tables["Article"].Columns["Key"], oDataSet.Tables["Lots"].Columns["Key"]);
                                LotsLot = oDataSet.Relations.Add("LotsLot", oDataSet.Tables["Lots"].Columns["Key"], oDataSet.Tables["Lot"].Columns["Key"]);
                            }
                            else
                            {
                                Detail = oDataSet.Relations.Add("DetailsLot", oDataSet.Tables["Article"].Columns["LineNum"], oDataSet.Tables["Lots"].Columns["LineNum"]);
                                LotsLot = oDataSet.Relations.Add("LotsLot", oDataSet.Tables["Lots"].Columns["Key"], oDataSet.Tables["Lot"].Columns["Key"]);
                            }

                            Document.Nested = true;
                            Details.Nested = true;
                            Detail.Nested = true;
                            LotsLot.Nested = true;

                         

                            oDataSet.Tables["Documents"].Columns["DocEntry"].ColumnMapping = MappingType.Hidden;
                            oDataSet.Tables["Articles"].Columns["DocEntry"].ColumnMapping = MappingType.Hidden;
                            oDataSet.Tables["Article"].Columns["DocEntry"].ColumnMapping = MappingType.Hidden;
                            if (strType == "Inventory")
                            {
                                oDataSet.Tables["Article"].Columns["Key"].ColumnMapping = MappingType.Hidden;
                                oDataSet.Tables["Lots"].Columns["Key"].ColumnMapping = MappingType.Hidden;
                            }
                            else
                            {
                                oDataSet.Tables["Article"].Columns["LineNum"].ColumnMapping = MappingType.Hidden;
                                oDataSet.Tables["Lots"].Columns["DocEntry"].ColumnMapping = MappingType.Hidden;
                                oDataSet.Tables["Lots"].Columns["LineNum"].ColumnMapping = MappingType.Hidden;
                            }
                            oDataSet.Tables["Lots"].Columns["Key"].ColumnMapping = MappingType.Hidden;
                            oDataSet.Tables["Lot"].Columns["Key"].ColumnMapping = MappingType.Hidden;


                     

                            for (int i = 0; i <= oDataSet.Tables["Documents"].Columns.Count - 1; i++)
                            {
                                DataTable Dt = GetFrenchName(oDataSet.Tables["Documents"].Columns[i].Caption);
                                if ((Dt != null) & Dt.Rows.Count > 0)
                                {
                                    oDataSet.Tables["Documents"].Columns[i].ColumnName = Dt.Rows[0][0].ToString();
                                }
                            }
                            oDataSet.AcceptChanges();

                            for (int i = 0; i <= oDataSet.Tables["Article"].Columns.Count - 1; i++)
                            {
                                DataTable Dt = GetFrenchName(oDataSet.Tables["Article"].Columns[i].Caption);
                                if ((Dt != null) & Dt.Rows.Count > 0)
                                {
                                    oDataSet.Tables["Article"].Columns[i].ColumnName = Dt.Rows[0][0].ToString();
                                }
                            }
                            oDataSet.AcceptChanges();

                            for (int i = 0; i <= oDataSet.Tables["Lot"].Columns.Count - 1; i++)
                            {
                                DataTable Dt = GetFrenchName(oDataSet.Tables["Lot"].Columns[i].Caption);
                                if ((Dt != null) & Dt.Rows.Count > 0)
                                {
                                    oDataSet.Tables["Lot"].Columns[i].ColumnName = Dt.Rows[0][0].ToString();
                                }
                            }
                            oDataSet.AcceptChanges();

                            addEmptyElementsToXML(oDataSet);

                          

                            DataTable FND = GetFrenchName("Documents");
                            if ((FND != null) & FND.Rows.Count > 0)
                            {
                                oDataSet.Tables["Documents"].TableName = FND.Rows[0][0].ToString();
                            }
                            oDataSet.AcceptChanges();

                            oDataSet.WriteXml(strDFNP);


                            XmlWriter w = new XmlTextWriter(strDFNP, Encoding.UTF8);
                            w.WriteProcessingInstruction("xml", "version='1.0' encoding='UTF-8'");
                            XmlDataDocument xd = new XmlDataDocument(oDataSet);
                            XmlDataDocument xdNew = new XmlDataDocument();
                            oDataSet.EnforceConstraints = false;
                            XmlNode node = xdNew.ImportNode(xd.DocumentElement.LastChild, true);
                            node.WriteTo(w);
                            w.Close();

                          
                            if (File.Exists(strDFNP))
                            {
                                if (strType == "GRPO")
                                {
                                    sQuery = "Update OPDN Set U_Export='Y' Where DocEntry=" + intDocEntry;
                                }
                                else if (strType == "GR")
                                {
                                    sQuery = "Update ORPD Set U_Export='Y' Where DocEntry=" + intDocEntry;
                                }
                                else if (strType == "APCreditMemo")
                                {
                                    sQuery = "Update ORPC Set U_Export='Y' Where DocEntry=" + intDocEntry;
                                }
                                else if (strType == "Inventory")
                                {
                                    sQuery = "Update OIQR Set U_Export='Y' Where DocEntry=" + intDocEntry;
                                }
                                oRecordSet.DoQuery(sQuery);

                                SAPbobsCOM.UserTable oUserTable;
                                oUserTable = (SAPbobsCOM.UserTable)oCompany.UserTables.Item("Z_HDF_OBND_LOG");

                                sQuery = "Select count(*) As Code From [@Z_HDF_OBND_Log]";
                                oRecordSet.DoQuery(sQuery);
                                //Set default, mandatory fields
                                if (oRecordSet.RecordCount > 0)
                                {
                                    oUserTable.Code = (Convert.ToInt32(oRecordSet.Fields.Item("Code").Value) + 1).ToString();
                                    oUserTable.Name = (Convert.ToInt32(oRecordSet.Fields.Item("Code").Value) + 1).ToString();
                                }
                                else
                                {
                                    oUserTable.Code = "1";
                                    oUserTable.Name = "1";
                                }
                                //Set user field
                                oUserTable.UserFields.Fields.Item("U_Type").Value = strType;
                                oUserTable.UserFields.Fields.Item("U_DocNum").Value = intDocEntry.ToString();
                                oUserTable.UserFields.Fields.Item("U_Status").Value = "Y";
                                DateTime now = DateTime.Now;
                                oUserTable.UserFields.Fields.Item("U_ProDate").Value = now.ToString("d");
                                oUserTable.UserFields.Fields.Item("U_ProTime").Value = now.ToString("HH:MM");
                                oUserTable.UserFields.Fields.Item("U_Remarks").Value = "Exported Successfully";
                                oUserTable.Add();

                                traceService("Success");
                            }
                            else
                            {

                                SAPbobsCOM.UserTable oUserTable;
                                oUserTable = (SAPbobsCOM.UserTable)oCompany.UserTables.Item("Z_HDF_OBND_LOG");

                                sQuery = "Select count(*) As Code From [@Z_HDF_OBND_Log]";
                                oRecordSet.DoQuery(sQuery);
                                //Set default, mandatory fields
                                if (oRecordSet.RecordCount > 0)
                                {
                                    oUserTable.Code = (Convert.ToInt32(oRecordSet.Fields.Item("Code").Value) + 1).ToString();
                                    oUserTable.Name = (Convert.ToInt32(oRecordSet.Fields.Item("Code").Value) + 1).ToString();
                                }
                                else
                                {
                                    oUserTable.Code = "1";
                                    oUserTable.Name = "1";
                                }
                                //Set user field
                                oUserTable.UserFields.Fields.Item("U_Type").Value = strType;
                                oUserTable.UserFields.Fields.Item("U_DocNum").Value = strDocNum;
                                oUserTable.UserFields.Fields.Item("U_Status").Value = "N";
                                DateTime now = DateTime.Now;
                                oUserTable.UserFields.Fields.Item("U_ProDate").Value = now.ToString("d");
                                oUserTable.UserFields.Fields.Item("U_ProTime").Value = now.ToString("HH:MM");
                                oUserTable.UserFields.Fields.Item("U_Remarks").Value = oCompany.GetLastErrorDescription().ToString();
                                oUserTable.Add();

                                traceService("Failed");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                traceService(ex.StackTrace);
                traceService(ex.Message);
            }
        }

        public DataTable ExecuteReader(string strConnection,string strQuery)
        {
            DataTable functionReturnValue = null;
            SqlConnection myConnection = new SqlConnection(strConnection);
            Ds = new DataSet();
            try
            {
                myConnection.Open();
                oSqlAdap = new SqlDataAdapter(strQuery, myConnection);
                oSqlAdap.Fill(Ds, "T_Temp");
                functionReturnValue = Ds.Tables["T_Temp"];
            }
            catch (Exception ex)
            {
                myConnection.Close();
            }
            finally
            {
                myConnection.Close();
                myConnection = null;
                oSqlAdap = null;
            }
            return functionReturnValue;
        }

        public DataSet ExecuteDataSet(string strConnection, string strQuery)
        {
            DataSet functionReturnValue = null;
            SqlConnection myConnection = new SqlConnection(strConnection);
            Ds = new DataSet();
            try
            {
                myConnection.Open();
                oSqlAdap = new SqlDataAdapter(strQuery, myConnection);
                oSqlAdap.Fill(Ds, "T_Temp");
                functionReturnValue = Ds;
            }
            catch (Exception ex)
            {
                myConnection.Close();
            }
            finally
            {
                myConnection.Close();
                myConnection = null;
                oSqlAdap = null;
            }
            return functionReturnValue;
        }

        public string GetFilePath(string Type)
        {
            string _retVal = null;
            try
            {
                oRecordSet = (SAPbobsCOM.Recordset)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);
                oRecordSet.DoQuery("Select T1.U_ExpPath From [@Z_HDF_OBND] T1 Where T1.U_Type= '" + Type + "'");
                if (!oRecordSet.EoF)
                {
                    _retVal = oRecordSet.Fields.Item(0).Value;
                }
                else
                {
                    throw new Exception("");
                }
            }
            catch (Exception)
            {
                throw;
            }
            return _retVal;
        }

        public DataTable GetFrenchName(string EnglishName)
        {
            string strMaiDB = System.Configuration.ConfigurationManager.AppSettings["MainDB"].ToString();
            string DBServer = System.Configuration.ConfigurationManager.AppSettings["SAPServer"].ToString();
            string DBUserName = System.Configuration.ConfigurationManager.AppSettings["DbUserName"].ToString();
            string DBPwd = System.Configuration.ConfigurationManager.AppSettings["DbPassword"].ToString();
            object[] args = { DBServer, strMaiDB, DBUserName, DBPwd };

            string strConnection = string.Format(ConfigurationManager.AppSettings["Logger"].ToString(), args);
            SqlConnection myConnection = new SqlConnection(strConnection);

            myConnection = new SqlConnection(strConnection);
            DataTable _retVal = new DataTable();

            try
            {
                myConnection.Open();
                oCommand = new SqlCommand();
                if (myConnection.State == ConnectionState.Open)
                {
                    oCommand.Connection = myConnection;
                    string strquery = "Select T1.U_FName From [@Z_HDF_OBD1] T1 Where T1.U_EName='" + EnglishName + "'";
                    oCommand.CommandText = strquery;
                    oCommand.CommandType = CommandType.Text;
                    oSqlAdap = new SqlDataAdapter(oCommand);
                    oSqlAdap.Fill(_retVal);
                }
                else
                {
                    throw new Exception("");
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                myConnection.Close();
                oCommand = null;
                oSqlAdap = null;
            }
            return _retVal;
        }

        private void addEmptyElementsToXML(DataSet dataSet)
        {
            try
            {
                foreach (DataTable dataTable in dataSet.Tables)
                {
                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        for (int j = 0; j <= dataRow.ItemArray.Length - 1; j++)
                        {
                            if (dataRow[j] == System.DBNull.Value)
                            {
                                dataRow[j] = string.Empty;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}




