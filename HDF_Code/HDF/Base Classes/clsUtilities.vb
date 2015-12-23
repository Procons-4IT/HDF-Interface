
Imports System.Collections.Specialized
'Imports CrystalDecisions.CrystalReports.Engine
'Imports CrystalDecisions.Shared
Imports System.Drawing
Imports System.IO
Imports System.Threading


Public Class clsUtilities


    Private strThousSep As String = ","
    Private strDecSep As String = "."
    Private intQtyDec As Integer = 3
    Private FormNum As Integer
    ' Dim cryRpt As New ReportDocument
    Private ds As New Barcode       '(dataset)
    Private oDRow As DataRow
    Public strFilterFile As String = String.Empty


    Public Sub New()
        MyBase.New()
        FormNum = 1
    End Sub

    Public Sub setEditText(ByVal aForm As SAPbouiCOM.Form, ByVal aItem As String, ByVal aValue As String)
        Dim oEdit As SAPbouiCOM.EditText
        oEdit = aForm.Items.Item(aItem).Specific
        oEdit.String = aValue
    End Sub


    Public Function generateBarCodes(ByVal aform As SAPbouiCOM.Form) As Boolean
        Dim strFromItem, strToItem, strBrand, strSeason, strSQL As String
        Dim ostatic As SAPbouiCOM.StaticText
        Dim oTempRec As SAPbobsCOM.Recordset
        Try
            strFromItem = getEditTextvalue(aform, "4")
            strToItem = getEditTextvalue(aform, "6")
            Dim oCombo As SAPbouiCOM.ComboBox
            oCombo = aform.Items.Item("8").Specific
            Try
                strSeason = oCombo.Selected.Value ' getEditTextvalue(aform, "8")
            Catch ex As Exception
                strSeason = ""
            End Try

            oCombo = aform.Items.Item("10").Specific
            Try
                strBrand = oCombo.Selected.Value ' getEditTextvalue(aform, "10")
            Catch ex As Exception
                strBrand = ""
            End Try

            If strFromItem = "" Then
                strFromItem = " (1=1"
            Else
                strFromItem = "( ""ItemCode"" >='" & strFromItem & "'"
            End If

            If strToItem = "" Then
                strFromItem = strFromItem & " and  1=1 )"
            Else
                strFromItem = strFromItem & " and  ""ItemCode"" <='" & strToItem & "')"
            End If
            If strSeason = "" Then
                strSeason = " and 1=1"
            Else
                strSeason = " and ""U_SEASON""='" & strSeason & "'"
            End If
            If strBrand = "" Then
                strBrand = " and 1=1"
            Else
                strBrand = " and ""U_BRAND""='" & strBrand & "'"
            End If
            strSQL = "Select ""ItemCode"",""ItemName"" from OITM where " & strFromItem & strSeason & strBrand
            oTempRec = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
            oTempRec.DoQuery(strSQL)
            ostatic = aform.Items.Item("11").Specific

            Dim ORec As SAPbobsCOM.Recordset
            ORec = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
            ORec.DoQuery("Select IFNULL(""U_S_BINCODE"",'') from OADM")
            Dim aBinCode As String
            aBinCode = ORec.Fields.Item(0).Value
            If aBinCode = "" Then
                oApplication.Utilities.Message("BinCode not defined in the Company Setup", SAPbouiCOM.BoStatusBarMessageType.smt_Error)
                Return False
            Else
                aBinCode = aBinCode + getmaxBarCode("OBCD", "BcdCode")
                '  aBarCode = GenerateCheckDidgit(aBinCode)
            End If
            Dim intBarCode, aBarCode As String
            Dim dtTable As SAPbouiCOM.DataTable
            Dim ogrid As SAPbouiCOM.Grid

            ogrid = aform.Items.Item("12").Specific
            ogrid.DataTable = aform.DataSources.DataTables.Item("DT_0")
            aform.Items.Item("12").Visible = False
            dtTable = aform.DataSources.DataTables.Item("DT_1")
            dtTable.Rows.Clear()
            ' dtTable.ExecuteQuery("Select ItemCode,ItemName,CodeBars from OITM where ItemCode='dd'")
            For intRow As Integer = 0 To oTempRec.RecordCount - 1
                ostatic.Caption = "Processing ItemCode : " & oTempRec.Fields.Item(0).Value
                '     GenerateBarCode(oTempRec.Fields.Item(0).Value, "test")
                ORec.DoQuery("Select * from OBCD where ""ItemCode""='" & oTempRec.Fields.Item(0).Value & "'")
                If ORec.RecordCount <= 0 Then

                    aBarCode = GenerateCheckDidgit(aBinCode)
                    aBinCode = Convert.ToDouble(aBinCode) + 1
                    dtTable.Rows.Add()
                    dtTable.SetValue(0, dtTable.Rows.Count - 1, oTempRec.Fields.Item(0).Value)
                    dtTable.SetValue(1, dtTable.Rows.Count - 1, oTempRec.Fields.Item(1).Value)
                    dtTable.SetValue(2, dtTable.Rows.Count - 1, aBarCode)
                    ostatic.Caption = "Processing ItemCode : " & oTempRec.Fields.Item(0).Value & "BarCode : " & aBarCode
                End If
                oTempRec.MoveNext()
            Next
            ostatic.Caption = "Barcode prepared successfully"
            ogrid = aform.Items.Item("12").Specific
            ogrid.DataTable = dtTable
            ogrid.Columns.Item(0).TitleObject.Caption = "Item Code"
            Dim oedittext As SAPbouiCOM.EditTextColumn
            oedittext = ogrid.Columns.Item(0)
            oedittext.LinkedObjectType = "4"
            ogrid.Columns.Item(1).TitleObject.Caption = "Item Name"
            ogrid.Columns.Item(2).TitleObject.Caption = "BarCode"
            ogrid.AutoResizeColumns()
            ogrid.SelectionMode = SAPbouiCOM.BoMatrixSelect.ms_None
            aform.Items.Item("12").Visible = True
            Return True
        Catch ex As Exception
            Message(ex.Message, SAPbouiCOM.BoStatusBarMessageType.smt_Error)
            Return False
        End Try
    End Function


    Public Function PrintBarCode(ByVal aform As SAPbouiCOM.Form) As Boolean
        Dim strFromItem, strToItem, strBrand, strSeason, strSQL As String
        Dim ostatic As SAPbouiCOM.StaticText
        Dim oTempRec As SAPbobsCOM.Recordset
        Try
            strFromItem = getEditTextvalue(aform, "4")
            strToItem = getEditTextvalue(aform, "6")
            Dim oCombo As SAPbouiCOM.ComboBox
            oCombo = aform.Items.Item("8").Specific
            Try
                strSeason = oCombo.Selected.Value ' getEditTextvalue(aform, "8")
            Catch ex As Exception
                strSeason = ""
            End Try

            oCombo = aform.Items.Item("10").Specific
            Try
                strBrand = oCombo.Selected.Value ' getEditTextvalue(aform, "10")
            Catch ex As Exception
                strBrand = ""
            End Try

            If strFromItem = "" Then
                strFromItem = " (1=1"
            Else
                strFromItem = "( ""ItemCode"" >='" & strFromItem & "'"
            End If

            If strToItem = "" Then
                strFromItem = strFromItem & " and  1=1 )"
            Else
                strFromItem = strFromItem & " and  ""ItemCode"" <='" & strToItem & "')"
            End If
            If strSeason = "" Then
                strSeason = " and 1=1"
            Else
                strSeason = " and ""U_SEASON""='" & strSeason & "'"
            End If
            If strBrand = "" Then
                strBrand = " and 1=1"
            Else
                strBrand = " and ""U_BRAND""='" & strBrand & "'"
            End If
            strSQL = "Select ""CodeBars"" ""BarCode"",""ItemCode"",""ItemName"",""U_SEASON"",""U_BRAND""  from OITM where " & strFromItem & strSeason & strBrand
            Dim ogrid As SAPbouiCOM.Grid
            ogrid = aform.Items.Item("12").Specific
            ogrid.DataTable = aform.DataSources.DataTables.Item("DT_0")
            ogrid.DataTable.ExecuteQuery(strSQL)
            aform.Items.Item("12").Visible = False
            ogrid = aform.Items.Item("12").Specific
            ogrid.DataTable.ExecuteQuery(strSQL)
            ogrid.Columns.Item("ItemCode").TitleObject.Caption = "Item Code"
            ogrid.Columns.Item("U_SEASON").TitleObject.Caption = "Season"
            ogrid.Columns.Item("U_BRAND").TitleObject.Caption = "Brand"
            Dim oedittext As SAPbouiCOM.EditTextColumn
            oedittext = ogrid.Columns.Item("ItemCode")
            oedittext.LinkedObjectType = "4"
            ogrid.AutoResizeColumns()
            ogrid.SelectionMode = SAPbouiCOM.BoMatrixSelect.ms_None
            aform.Items.Item("12").Visible = True
            Return True
        Catch ex As Exception
            Message(ex.Message, SAPbouiCOM.BoStatusBarMessageType.smt_Error)
            Return False
        End Try
    End Function

    Public Function PrintBarCode_PO(ByVal aform As SAPbouiCOM.Form) As Boolean
        Dim strFromItem, strToItem, strBrand, strSeason, strSQL As String
        Dim ostatic As SAPbouiCOM.StaticText
        Dim oTempRec As SAPbobsCOM.Recordset
        Try
            strFromItem = getEditTextvalue(aform, "4")
            strToItem = getEditTextvalue(aform, "6")
            Dim oCombo As SAPbouiCOM.ComboBox
            oCombo = aform.Items.Item("8").Specific
            Try
                strSeason = oCombo.Selected.Value ' getEditTextvalue(aform, "8")
            Catch ex As Exception
                strSeason = ""
            End Try

            oCombo = aform.Items.Item("10").Specific
            Try
                strBrand = oCombo.Selected.Value ' getEditTextvalue(aform, "10")
            Catch ex As Exception
                strBrand = ""
            End Try

            If strFromItem = "" Then
                strFromItem = " (1=1"
            Else
                strFromItem = "( T0.""DocEntry"" ='" & strFromItem & "')"
            End If

            'If strToItem = "" Then
            '    strFromItem = strFromItem & " and  1=1 )"
            'Else
            '    strFromItem = strFromItem & " and  T0.""DocEntry"" <='" & strToItem & "')"
            'End If
            If strSeason = "" Then
                strSeason = " and 1=1"
            Else
                strSeason = " and ""U_SEASON""='" & strSeason & "'"
            End If
            If strBrand = "" Then
                strBrand = " and 1=1"
            Else
                strBrand = " and ""U_BRAND""='" & strBrand & "'"
            End If
            'strSQL = "Select ""CodeBars"" ""BarCode"",""ItemCode"",""ItemName"",""U_SEASON"" from OITM where " & strFromItem & strSeason & strBrand
            strSQL = "SELECT T1.""CodeBars"" ""BarCode"",T1.""ItemCode"",""ItemName"",T1.""Quantity"",T2.""U_SEASON"",T2.""U_BRAND"" FROM OPOR T0  INNER JOIN POR1 T1 ON T0.""DocEntry"" = T1.""DocEntry"" INNER JOIN OITM T2 ON T1.""ItemCode"" = T2.""ItemCode"" where " & strFromItem

            Dim ogrid As SAPbouiCOM.Grid
            ogrid = aform.Items.Item("12").Specific
            ogrid.DataTable = aform.DataSources.DataTables.Item("DT_0")
            ogrid.DataTable.ExecuteQuery(strSQL)
            aform.Items.Item("12").Visible = False
            ogrid = aform.Items.Item("12").Specific
            ogrid.DataTable.ExecuteQuery(strSQL)
            ogrid.Columns.Item("U_SEASON").TitleObject.Caption = "Season"
            ogrid.Columns.Item("U_BRAND").TitleObject.Caption = "Brand"
            Dim oedittext As SAPbouiCOM.EditTextColumn
            oedittext = ogrid.Columns.Item("ItemCode")
            oedittext.LinkedObjectType = "4"
            ogrid.AutoResizeColumns()
            ogrid.SelectionMode = SAPbouiCOM.BoMatrixSelect.ms_None
            aform.Items.Item("12").Visible = True
            Return True
        Catch ex As Exception
            Message(ex.Message, SAPbouiCOM.BoStatusBarMessageType.smt_Error)
            Return False
        End Try
    End Function

    Public Sub PrintbarCode_Report_PO(ByVal aform As SAPbouiCOM.Form)
        Dim oRec, oRecTemp, oRecBP, oBalanceRs, oTemp As SAPbobsCOM.Recordset
        Dim strfrom, dtPosting, dtdue, dttax, strPaySQL, strto, strBranch, strSlpCode, strSlpName, strSMNo, strFromBP, strToBP, straging, strCardcode, strCardname, strBlock, strCity, strBilltoDef, strZipcode, strAddress, strCounty, strPhone1, strFax, strCntctprsn, strTerrtory, strNotes As String
        Dim dtFrom, dtTo, dtAging As Date
        Dim intReportChoice As Integer
        Dim dblRef1, dblCredit, dblDebit, dblCumulative, dblOpenBalance As Double

        Dim intMonth, intYear As Integer
        Dim strCode, strSQL, strMonth, strYear, strType, strCmpCode As String
        Dim oRS As SAPbobsCOM.Recordset
        Dim dblTotal As Double = 0

        Dim strFromItem, strToItem, strBrand, strSeason As String
        Dim ostatic As SAPbouiCOM.StaticText
        Dim oTempRec As SAPbobsCOM.Recordset
        Try
            strFromItem = getEditTextvalue(aform, "4")
            strToItem = getEditTextvalue(aform, "6")
            Dim oCombo As SAPbouiCOM.ComboBox
            oCombo = aform.Items.Item("8").Specific
            Try
                strSeason = oCombo.Selected.Value ' getEditTextvalue(aform, "8")
            Catch ex As Exception
                strSeason = ""
            End Try

            oCombo = aform.Items.Item("10").Specific
            Try
                strBrand = oCombo.Selected.Value ' getEditTextvalue(aform, "10")
            Catch ex As Exception
                strBrand = ""
            End Try

            If strFromItem = "" Then
                strFromItem = " (1=1"
            Else
                strFromItem = "( T0.""DocEntry"" ='" & strFromItem & "')"
            End If

            'If strToItem = "" Then
            '    strFromItem = strFromItem & " and  1=1 )"
            'Else
            '    strFromItem = strFromItem & " and  ""DocEntry"" <='" & strToItem & "')"
            'End If
            'If strSeason = "" Then
            '    strSeason = " and 1=1"
            'Else
            '    strSeason = " and ""U_SEASON""='" & strSeason & "'"
            'End If
            'If strBrand = "" Then
            '    strBrand = " and 1=1"
            'Else
            '    strBrand = " and ""U_BRAND""='" & strBrand & "'"
            'End If
            '  strSQL = "Select ""CodeBars"" ""BarCode"",""ItemCode"",""ItemName"",""U_SEASON"" from OITM where " & strFromItem & strSeason & strBrand
            ' strSQL = "SELECT T1.""CodeBars"" ""BarCode"",""ItemCode"",""ItemName"",T2.""U_SEASON"" FROM OPO""CodeBars"" ""BarCode"",""ItemCode"",""ItemName"",""U_SEASON""R T0  INNER JOIN POR1 T1 ON T0.""DocEntry"" = T1.""DocEntry"" INNER JOIN OITM T2 ON T1.""ItemCode"" = T2.""ItemCode"" where " & strFromItem
            strSQL = "SELECT T1.""CodeBars"" ""BarCode"",T1.""ItemCode"",""ItemName"",T2.""U_SEASON"",T1.""Quantity"",T2.""U_BRAND"" FROM OPOR T0  INNER JOIN POR1 T1 ON T0.""DocEntry"" = T1.""DocEntry"" INNER JOIN OITM T2 ON T1.""ItemCode"" = T2.""ItemCode"" where " & strFromItem
        Catch ex As Exception
            Message(ex.Message, SAPbouiCOM.BoStatusBarMessageType.smt_Error)
        End Try

        oApplication.Utilities.Message("Processing...", SAPbouiCOM.BoStatusBarMessageType.smt_Warning)
        oRec = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
        oTemp = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
        oRecBP = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
        oRecBP.DoQuery(strSQL)

        If 1 = 2 Then ' oRec.RecordCount <= 0 Then
            oApplication.Utilities.Message("Payroll not generated for selected month and year", SAPbouiCOM.BoStatusBarMessageType.smt_Error)
            Exit Sub
        Else
            ds.Clear()
            ds.Clear()
            oTemp.DoQuery(strSQL)
            Dim dblQuantity As Double
            For introw As Integer = 0 To oTemp.RecordCount - 1
                dblQuantity = oTemp.Fields.Item("Quantity").Value
                For intLoop As Integer = 1 To dblQuantity


                    oDRow = ds.Tables("Barcode").NewRow()
                    oDRow.Item("Barcode") = oTemp.Fields.Item("BarCode").Value
                    oDRow.Item("ItemCode") = oTemp.Fields.Item("ItemCode").Value
                    oDRow.Item("ItemName") = oTemp.Fields.Item("ItemName").Value
                    oDRow.Item("Season") = oTemp.Fields.Item("U_SEASON").Value
                    '  MsgBox(oTemp.Fields.Item("U_BRAND").Value)
                    oDRow.Item("Brand") = oTemp.Fields.Item("U_BRAND").Value
                    oDRow.Item("Quantity") = 1 ' oTemp.Fields.Item("Quantity").Value
                    oRecBP.DoQuery("Select * from ""@S_OBPR""")
                    If oRecBP.RecordCount > 0 Then
                        oRecBP.DoQuery("Select * from ITM1 where ""ItemCode""='" & oTemp.Fields.Item("ItemCode").Value & "' and ""PriceList""=" & oRecBP.Fields.Item("Code").Value)
                    Else
                        oRecBP.DoQuery("Select * from ITM1 where ""ItemCode""='" & oTemp.Fields.Item("ItemCode").Value & "' and ""PriceList""=12")
                    End If

                    If oRecBP.RecordCount > 0 Then
                        oDRow.Item("Price") = oRecBP.Fields.Item("Price").Value
                    Else
                        oDRow.Item("Price") = 0
                    End If

                    ds.Tables("Barcode").Rows.Add(oDRow)
                Next
                oTemp.MoveNext()
            Next
            '   addCrystal(ds, "BarCode")
        End If
        ' oApplication.Utilities.Message("", SAPbouiCOM.BoStatusBarMessageType.smt_None)
    End Sub

    Public Sub PrintbarCode_Report(ByVal aform As SAPbouiCOM.Form)
        Dim oRec, oRecTemp, oRecBP, oBalanceRs, oTemp As SAPbobsCOM.Recordset
        Dim strfrom, dtPosting, dtdue, dttax, strPaySQL, strto, strBranch, strSlpCode, strSlpName, strSMNo, strFromBP, strToBP, straging, strCardcode, strCardname, strBlock, strCity, strBilltoDef, strZipcode, strAddress, strCounty, strPhone1, strFax, strCntctprsn, strTerrtory, strNotes As String
        Dim dtFrom, dtTo, dtAging As Date
        Dim intReportChoice As Integer
        Dim dblRef1, dblCredit, dblDebit, dblCumulative, dblOpenBalance As Double

        Dim intMonth, intYear As Integer
        Dim strCode, strSQL, strMonth, strYear, strType, strCmpCode As String
        Dim oRS As SAPbobsCOM.Recordset
        Dim dblTotal As Double = 0

        Dim strFromItem, strToItem, strBrand, strSeason As String
        Dim ostatic As SAPbouiCOM.StaticText
        Dim oTempRec As SAPbobsCOM.Recordset
        Try
            strFromItem = getEditTextvalue(aform, "4")
            strToItem = getEditTextvalue(aform, "6")
            Dim oCombo As SAPbouiCOM.ComboBox
            oCombo = aform.Items.Item("8").Specific
            Try
                strSeason = oCombo.Selected.Value ' getEditTextvalue(aform, "8")
            Catch ex As Exception
                strSeason = ""
            End Try

            oCombo = aform.Items.Item("10").Specific
            Try
                strBrand = oCombo.Selected.Value ' getEditTextvalue(aform, "10")
            Catch ex As Exception
                strBrand = ""
            End Try

            If strFromItem = "" Then
                strFromItem = " (1=1"
            Else
                strFromItem = "( ""ItemCode"" >='" & strFromItem & "'"
            End If

            If strToItem = "" Then
                strFromItem = strFromItem & " and  1=1 )"
            Else
                strFromItem = strFromItem & " and  ""ItemCode"" <='" & strToItem & "')"
            End If
            If strSeason = "" Then
                strSeason = " and 1=1"
            Else
                strSeason = " and ""U_SEASON""='" & strSeason & "'"
            End If
            If strBrand = "" Then
                strBrand = " and 1=1"
            Else
                strBrand = " and ""U_BRAND""='" & strBrand & "'"
            End If
            strSQL = "Select ""CodeBars"" ""BarCode"",""ItemCode"",""ItemName"",""U_SEASON"",""U_BRAND"" from OITM where " & strFromItem & strSeason & strBrand

        Catch ex As Exception
            Message(ex.Message, SAPbouiCOM.BoStatusBarMessageType.smt_Error)

        End Try

        oApplication.Utilities.Message("Processing...", SAPbouiCOM.BoStatusBarMessageType.smt_Warning)
        oRec = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
        oTemp = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
        oRecBP = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
        oRecBP.DoQuery(strSQL)

        If 1 = 2 Then ' oRec.RecordCount <= 0 Then
            oApplication.Utilities.Message("Payroll not generated for selected month and year", SAPbouiCOM.BoStatusBarMessageType.smt_Error)
            Exit Sub
        Else
            ds.Clear()
            ds.Clear()
            oTemp.DoQuery(strSQL)
            For introw As Integer = 0 To oTemp.RecordCount - 1
                oDRow = ds.Tables("Barcode").NewRow()
                oDRow.Item("Barcode") = oTemp.Fields.Item("BarCode").Value
                oDRow.Item("ItemCode") = oTemp.Fields.Item("ItemCode").Value
                oDRow.Item("ItemName") = oTemp.Fields.Item("ItemName").Value
                oDRow.Item("Season") = oTemp.Fields.Item("U_SEASON").Value
                oDRow.Item("Brand") = oTemp.Fields.Item("U_BRAND").Value

                ' oRecBP.DoQuery("Select * from ITM1 where ItemCode='" & oTemp.Fields.Item("ItemCode").Value & "' and PriceList=1")
                oRecBP.DoQuery("Select * from ""@S_OBPR""")
                If oRecBP.RecordCount > 0 Then
                    oRecBP.DoQuery("Select * from ITM1 where ""ItemCode""='" & oTemp.Fields.Item("ItemCode").Value & "' and ""PriceList""=" & oRecBP.Fields.Item("Code").Value)
                Else
                    oRecBP.DoQuery("Select * from ITM1 where ""ItemCode""='" & oTemp.Fields.Item("ItemCode").Value & "' and ""PriceList""=11")
                End If
                If oRecBP.RecordCount > 0 Then
                    oDRow.Item("Price") = oRecBP.Fields.Item("Price").Value
                Else
                    oDRow.Item("Price") = 0
                End If

                oDRow.Item("Quantity") = 1
                ds.Tables("Barcode").Rows.Add(oDRow)
                oTemp.MoveNext()
            Next
            '  addCrystal(ds, "BarCode")
        End If
        ' oApplication.Utilities.Message("", SAPbouiCOM.BoStatusBarMessageType.smt_None)
    End Sub
    'Private Sub addCrystal(ByVal ds1 As DataSet, ByVal aChoice As String)
    '    Dim strFilename, stfilepath As String
    '    Dim strReportFileName As String
    '    Dim strCompany As String
    '    strCompany = oApplication.Company.CompanyDB
    '    If aChoice = "BarCode" Then
    '        If strBarCodeFormat = "G" Then
    '            strReportFileName = "rptBarcode_" & strCompany & ".rpt"
    '        Else
    '            strReportFileName = "rptBarcode_Accessories_" & strCompany & ".rpt"
    '        End If

    '        strFilename = System.Windows.Forms.Application.StartupPath & "\BarCode"
    '    ElseIf aChoice = "Agreement" Then
    '        strReportFileName = "Agreement.rpt"
    '        strFilename = System.Windows.Forms.Application.StartupPath & "\Rental_Agreement"
    '    Else
    '        strReportFileName = "AcctStatement.rpt"
    '        strFilename = System.Windows.Forms.Application.StartupPath & "\AccountStatement"
    '    End If
    '    strReportFileName = strReportFileName
    '    strFilename = strFilename & ".pdf"
    '    stfilepath = System.Windows.Forms.Application.StartupPath & "\Reports\" & strReportFileName
    '    If File.Exists(stfilepath) = False Then
    '        oApplication.Utilities.Message("Report does not exists", SAPbouiCOM.BoStatusBarMessageType.smt_Error)
    '        Exit Sub
    '    End If
    '    If File.Exists(strFilename) Then
    '        File.Delete(strFilename)
    '    End If
    '    ' If ds1.Tables.Item("AccountBalance").Rows.Count > 0 Then
    '    If 1 = 1 Then
    '        cryRpt.Load(System.Windows.Forms.Application.StartupPath & "\Reports\" & strReportFileName)
    '        cryRpt.SetDataSource(ds1)
    '        If "T" = "T" Then
    '            Dim mythread As New System.Threading.Thread(AddressOf openFileDialog)
    '            mythread.SetApartmentState(ApartmentState.STA)
    '            mythread.Start()
    '            mythread.Join()
    '            ds1.Clear()
    '        Else
    '            Dim CrExportOptions As ExportOptions
    '            Dim CrDiskFileDestinationOptions As New  _
    '            DiskFileDestinationOptions()
    '            Dim CrFormatTypeOptions As New PdfRtfWordFormatOptions()
    '            CrDiskFileDestinationOptions.DiskFileName = strFilename
    '            CrExportOptions = cryRpt.ExportOptions
    '            With CrExportOptions
    '                .ExportDestinationType = ExportDestinationType.DiskFile
    '                .ExportFormatType = ExportFormatType.PortableDocFormat
    '                .DestinationOptions = CrDiskFileDestinationOptions
    '                .FormatOptions = CrFormatTypeOptions
    '            End With
    '            cryRpt.Export()
    '            cryRpt.Close()
    '            Dim x As System.Diagnostics.ProcessStartInfo
    '            x = New System.Diagnostics.ProcessStartInfo
    '            x.UseShellExecute = True
    '            x.FileName = strFilename
    '            System.Diagnostics.Process.Start(x)
    '            x = Nothing
    '            ' objUtility.ShowSuccessMessage("Report exported into PDF File")
    '        End If

    '    Else
    '        ' objUtility.ShowWarningMessage("No data found")
    '    End If

    'End Sub
    'Private Sub openFileDialog()
    '    Dim objPL As New frmReportViewer
    '    objPL.iniViewer = AddressOf objPL.GenerateReport
    '    objPL.rptViewer.ReportSource = cryRpt
    '    objPL.rptViewer.Refresh()
    '    objPL.WindowState = FormWindowState.Maximized
    '    objPL.ShowDialog()
    '    System.Threading.Thread.CurrentThread.Abort()
    'End Sub
    Public Function GenerateBarCode_Bulk(ByVal aItemCode As String, ByVal aBarCode As String) As String
        Dim ORec As SAPbobsCOM.Recordset
        Dim aBinCode As String
        Try
            ORec = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
            ORec.DoQuery("Select * from OBCD where ""ItemCode""='" & aItemCode & "'")
            If ORec.RecordCount > 0 Then
                ' Message("Barcode already exists", SAPbouiCOM.BoStatusBarMessageType.smt_Error)
                Return ""
            Else
                ORec.DoQuery("Select IFNULL(""U_S_BinCode"",'') from OADM")
                aBinCode = ORec.Fields.Item(0).Value
                aBarCode = ""
                If aBinCode = "" Then
                    oApplication.Utilities.Message("BinCode not defined in the Company Setup", SAPbouiCOM.BoStatusBarMessageType.smt_Error)
                    Return ""
                Else
                    ' aBinCode = aBinCode + getmaxBarCode("OBCD", "BcdCode")
                    aBarCode = GenerateCheckDidgit(aBinCode)
                End If
                Return aBarCode

                'If AddBarCode(aItemCode, aBarCode) = True Then
                '    Return True
                'Else
                '    Return False
                'End If
            End If
        Catch ex As Exception
            Message(ex.Message, SAPbouiCOM.BoStatusBarMessageType.smt_Error)
            Return False
        End Try
    End Function
    Public Function GenerateBarCode(ByVal aItemCode As String, ByVal aBarCode As String) As Boolean
        Dim ORec As SAPbobsCOM.Recordset
        Dim aBinCode As String
        Try
            ORec = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
            ORec.DoQuery("Select * from OBCD where ""ItemCode""='" & aItemCode & "'")
            If ORec.RecordCount > 0 Then
                ' Message("Barcode already exists", SAPbouiCOM.BoStatusBarMessageType.smt_Error)
                Return True
            Else
                ORec.DoQuery("Select IFNULL(""U_S_BINCODE"",'') from OADM")
                aBinCode = ORec.Fields.Item(0).Value
                If aBinCode = "" Then
                    oApplication.Utilities.Message("BinCode not defined in the Company Setup", SAPbouiCOM.BoStatusBarMessageType.smt_Error)
                    Return False
                Else
                    aBinCode = aBinCode + getmaxBarCode("OBCD", "BcdCode")
                    aBarCode = GenerateCheckDidgit(aBinCode)
                End If
                If AddBarCode(aItemCode, aBarCode) = True Then
                    Return True
                Else
                    Return False
                End If
            End If
        Catch ex As Exception
            Message(ex.Message, SAPbouiCOM.BoStatusBarMessageType.smt_Error)
            Return False
        End Try
    End Function


    Private Function GenerateCheckDidgit(ByVal aNumber As String) As String
        Dim strCheckDigit As String = "0"
        Dim intOdd, intEven, intvalue As Integer
        Dim strOdd, strEven As String
        intOdd = 0
        intEven = 0
        strOdd = ""
        strEven = ""
        For index As Integer = aNumber.Length - 1 To 0 Step -1 ' aNumber.Length - 1
            intvalue = aNumber.Substring(index, 1)
            Select Case index
                Case 1, 3, 5, 7, 9, 11
                    strOdd = strOdd & "," & aNumber.Substring(index, 1)
                    intOdd = intOdd + CInt(aNumber.Substring(index, 1))
                Case 0, 2, 4, 6, 8, 10
                    strEven = strEven & "," & aNumber.Substring(index, 1)
                    intEven = intEven + CInt(aNumber.Substring(index, 1))
            End Select
        Next

        Dim intCheckDigit As Integer = 0
        intCheckDigit = (10 - ((3 * intOdd + intEven) Mod 10)) Mod 10
        strCheckDigit = aNumber + intCheckDigit.ToString
        Return strCheckDigit
    End Function


    Public Function getmaxBarCode(ByVal sTable As String, ByVal sColumn As String) As String
        Dim oRS As SAPbobsCOM.Recordset
        Dim MaxCode As Integer
        Dim sCode As String
        Dim strSQL, aBinCode As String
        Try
            oRS = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
            oRS.DoQuery("Select IFNULL(""U_S_BINCODE"",'') from OADM")
            aBinCode = oRS.Fields.Item(0).Value
            strSQL = "SELECT IFNULL(MAX(Cast(CAST(subString(""BcdCode"",0,13) AS Varchar) as Numeric)),0) FROM OBCD where ""BcdCode"" like '" & aBinCode & "%'"
            ExecuteSQL(oRS, strSQL)

            If Convert.ToString(oRS.Fields.Item(0).Value).Length > 0 Then
                sCode = oRS.Fields.Item(0).Value ' + 1
                Try
                    sCode = sCode.Substring(6, 6)
                Catch ex As Exception

                End Try

                MaxCode = Convert.ToInt64(sCode) + 1
            Else
                MaxCode = 1
            End If

            sCode = Format(MaxCode, "000000")
            Return sCode
        Catch ex As Exception
            Throw ex
        Finally
            oRS = Nothing
        End Try
    End Function
    Public Function AddBarCode(ByVal aItemCode As String, ByVal aBarCode As String, Optional ByVal aUOMEntry As Integer = 0) As Boolean
        Dim lpCmpSer As SAPbobsCOM.ICompanyService
        Dim lpBCSer As SAPbobsCOM.IBarCodesService
        Dim lpBCPar As SAPbobsCOM.IBarCodeParams
        Dim lpBC As SAPbobsCOM.IBarCode
        Dim lRS As SAPbobsCOM.IRecordset
        Dim lUomEntry As Long, lBcdEntry As Long
        Dim oItem As SAPbobsCOM.Items
        oItem = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oItems)
        Dim oRec1 As SAPbobsCOM.Recordset
        oRec1 = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
        Try
            If oItem.GetByKey(aItemCode) Then
                lUomEntry = oItem.DefaultPurchasingUoMEntry
                lRS = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
                ' lUomEntry = aUOMEntry
                lpCmpSer = oApplication.Company.GetCompanyService
                lpBCSer = lpCmpSer.GetBusinessService(SAPbobsCOM.ServiceTypes.BarCodesService)
                lpBC = lpBCSer.GetDataInterface(SAPbobsCOM.BarCodesServiceDataInterfaces.bsBarCode)
                lpBC.ItemNo = aItemCode
                lpBC.UoMEntry = lUomEntry
                lpBC.BarCode = aBarCode
                lpBCPar = lpBCSer.Add(lpBC)
                oRec1.DoQuery("Select * from OBCD where ""ItemCode""='" & aItemCode & "' and ""BcdCode""='" & aBarCode & "'")
                If oRec1.RecordCount > 0 Then
                    If oItem.GetByKey(aItemCode) Then
                        oItem.BarCode = aBarCode
                        oItem.Update()
                    End If
                End If
            End If
            '  MsgBox(lpBCPar.AbsEntry)
        Catch ex As Exception
            Message(ex.Message, SAPbouiCOM.BoStatusBarMessageType.smt_Error)
            Return False
        End Try





        Return True
    End Function

#Region "Update LOC"
    Public Function GetBankBalance(ByVal aCode As String) As Double
        Dim oRec, oTest As SAPbobsCOM.Recordset
        Dim dblTotalLimit, dblUtilizedAmount As Double
        oRec = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
        oTest = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
        oTest.DoQuery("Select IFNULL(U_CreditLmt,0) 'U_CreditLmt' from DSC1 where BankCode='" & aCode & "'")
        oRec.DoQuery("Select sum(U_CreditAmt) from [@Z_OSCL] where U_SubBank='" & aCode & "' and U_Status='U'")
        dblTotalLimit = 0
        dblUtilizedAmount = 0
        dblTotalLimit = oTest.Fields.Item("U_CreditLmt").Value
        dblUtilizedAmount = oRec.Fields.Item(0).Value
        dblUtilizedAmount = dblTotalLimit - dblUtilizedAmount
        Return dblUtilizedAmount
    End Function

    Public Sub UpdateBankBalance()
        Dim oRec, oTest As SAPbobsCOM.Recordset
        Dim dblTotalLimit, dblUtilizedAmount As Double
        oRec = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
        oTest = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
        oTest.DoQuery("Select * from DSC1")
        For intRow As Integer = 0 To oTest.RecordCount - 1
            oRec.DoQuery("Select sum(U_CreditAmt) from [@Z_OSCL] where U_SubBank='" & oTest.Fields.Item("BankCode").Value & "'  and U_Status='U'")
            dblTotalLimit = 0
            dblUtilizedAmount = 0
            dblTotalLimit = oTest.Fields.Item("U_CreditLmt").Value
            dblUtilizedAmount = oRec.Fields.Item(0).Value
            dblUtilizedAmount = dblTotalLimit - dblUtilizedAmount
            oRec.DoQuery("Update DSC1 set U_CreditBal='" & dblUtilizedAmount & "' where BankCode='" & oTest.Fields.Item("BankCode").Value & "'")
            oTest.MoveNext()
        Next



    End Sub
#End Region

#Region "Connect to Company"
    Public Sub Connect()
        Dim strCookie As String
        Dim strConnectionContext As String

        Try
            strCookie = oApplication.Company.GetContextCookie
            strConnectionContext = oApplication.SBO_Application.Company.GetConnectionContext(strCookie)

            If oApplication.Company.SetSboLoginContext(strConnectionContext) <> 0 Then
                Throw New Exception("Wrong login credentials.")
            End If

            'Open a connection to company
            If oApplication.Company.Connect() <> 0 Then
                Throw New Exception("Cannot connect to company database. ")
            End If

        Catch ex As Exception
            Throw ex
        End Try
    End Sub
#End Region

#Region "Genral Functions"

#Region "Get MaxCode"
    Public Function getMaxCode(ByVal sTable As String, ByVal sColumn As String) As String
        Dim oRS As SAPbobsCOM.Recordset
        Dim MaxCode As Integer
        Dim sCode As String
        Dim strSQL As String
        Try
            strSQL = "SELECT MAX(CAST(" & sColumn & " AS Numeric)) FROM [" & sTable & "]"
            ExecuteSQL(oRS, strSQL)

            If Convert.ToString(oRS.Fields.Item(0).Value).Length > 0 Then
                MaxCode = oRS.Fields.Item(0).Value + 1
            Else
                MaxCode = 1
            End If

            sCode = Format(MaxCode, "00000000")
            Return sCode
        Catch ex As Exception
            Throw ex
        Finally
            oRS = Nothing
        End Try
    End Function
#End Region

#Region "Status Message"
    Public Sub Message(ByVal sMessage As String, ByVal StatusType As SAPbouiCOM.BoStatusBarMessageType)
        oApplication.SBO_Application.StatusBar.SetText(sMessage, SAPbouiCOM.BoMessageTime.bmt_Short, StatusType)
    End Sub
#End Region

#Region "Add Choose from List"
    Public Sub AddChooseFromList(ByVal FormUID As String, ByVal CFL_Text As String, ByVal CFL_Button As String, _
                                        ByVal ObjectType As SAPbouiCOM.BoLinkedObject, _
                                            Optional ByVal AliasName As String = "", Optional ByVal CondVal As String = "", _
                                                    Optional ByVal Operation As SAPbouiCOM.BoConditionOperation = SAPbouiCOM.BoConditionOperation.co_EQUAL)

        Dim oCFLs As SAPbouiCOM.ChooseFromListCollection
        Dim oCons As SAPbouiCOM.Conditions
        Dim oCon As SAPbouiCOM.Condition
        Dim oCFL As SAPbouiCOM.ChooseFromList
        Dim oCFLCreationParams As SAPbouiCOM.ChooseFromListCreationParams
        Try
            oCFLs = oApplication.SBO_Application.Forms.Item(FormUID).ChooseFromLists
            oCFLCreationParams = oApplication.SBO_Application.CreateObject( _
                                    SAPbouiCOM.BoCreatableObjectType.cot_ChooseFromListCreationParams)

            ' Adding 2 CFL, one for the button and one for the edit text.
            If ObjectType = SAPbouiCOM.BoLinkedObject.lf_Items Then
                oCFLCreationParams.MultiSelection = True
            Else
                oCFLCreationParams.MultiSelection = False
            End If

            oCFLCreationParams.ObjectType = ObjectType
            oCFLCreationParams.UniqueID = CFL_Text

            oCFL = oCFLs.Add(oCFLCreationParams)

            ' Adding Conditions to CFL1

            oCons = oCFL.GetConditions()

            If Not AliasName = "" Then
                oCon = oCons.Add()
                oCon.Alias = AliasName
                oCon.Operation = Operation
                oCon.CondVal = CondVal
                oCFL.SetConditions(oCons)
            End If

            oCFLCreationParams.UniqueID = CFL_Button
            oCFL = oCFLs.Add(oCFLCreationParams)

        Catch ex As Exception
            Throw ex
        End Try
    End Sub
#End Region

#Region "Get Linked Object Type"
    Public Function getLinkedObjectType(ByVal Type As SAPbouiCOM.BoLinkedObject) As String
        Return CType(Type, String)
    End Function

#End Region

#Region "Execute Query"
    Public Sub ExecuteSQL(ByRef oRecordSet As SAPbobsCOM.Recordset, ByVal SQL As String)
        Try
            If oRecordSet Is Nothing Then
                oRecordSet = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
            End If

            oRecordSet.DoQuery(SQL)

        Catch ex As Exception
            Throw ex
        End Try
    End Sub
#End Region

#Region "Get Application path"
    Public Function getApplicationPath() As String

        Return Application.StartupPath.Trim

        'Return IO.Directory.GetParent(Application.StartupPath).ToString
    End Function
#End Region

#Region "Date Manipulation"

#Region "Convert SBO Date to System Date"
    '********************************************************************
    'Type		            :   Public Procedure     
    'Name               	:	ConvertStrToDate
    'Parameter          	:   ByVal oDate As String, ByVal strFormat As String
    'Return Value       	:	
    'Author             	:	Manu
    'Created Date       	:	07/12/05
    'Last Modified By	    :	
    'Modified Date        	:	
    'Purpose             	:	To convert Date according to current culture info
    '********************************************************************
    Public Function ConvertStrToDate(ByVal strDate As String, ByVal strFormat As String) As DateTime
        Try
            Dim oDate As DateTime
            Dim ci As New System.Globalization.CultureInfo("en-GB", False)
            Dim newCi As System.Globalization.CultureInfo = CType(ci.Clone(), System.Globalization.CultureInfo)

            System.Threading.Thread.CurrentThread.CurrentCulture = newCi
            oDate = oDate.ParseExact(strDate, strFormat, ci.DateTimeFormat)

            Return oDate
        Catch ex As Exception
            Throw ex
        End Try

    End Function
#End Region

#Region " Get SBO Date Format in String (ddmmyyyy)"
    '********************************************************************
    'Type		            :   Public Procedure     
    'Name               	:	StrSBODateFormat
    'Parameter          	:   none
    'Return Value       	:	
    'Author             	:	Manu
    'Created Date       	:	
    'Last Modified By	    :	
    'Modified Date        	:	
    'Purpose             	:	To get date Format(ddmmyy value) as applicable to SBO
    '********************************************************************
    Public Function StrSBODateFormat() As String
        Try
            Dim rsDate As SAPbobsCOM.Recordset
            Dim strsql As String, GetDateFormat As String
            Dim DateSep As Char

            strsql = "Select DateFormat,DateSep from OADM"
            rsDate = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
            rsDate.DoQuery(strsql)
            DateSep = rsDate.Fields.Item(1).Value

            Select Case rsDate.Fields.Item(0).Value
                Case 0
                    GetDateFormat = "dd" & DateSep & "MM" & DateSep & "yy"
                Case 1
                    GetDateFormat = "dd" & DateSep & "MM" & DateSep & "yyyy"
                Case 2
                    GetDateFormat = "MM" & DateSep & "dd" & DateSep & "yy"
                Case 3
                    GetDateFormat = "MM" & DateSep & "dd" & DateSep & "yyyy"
                Case 4
                    GetDateFormat = "yyyy" & DateSep & "dd" & DateSep & "MM"
                Case 5
                    GetDateFormat = "dd" & DateSep & "MMM" & DateSep & "yyyy"
            End Select
            Return GetDateFormat

        Catch ex As Exception
            Throw ex
        End Try
    End Function
#End Region

#Region "Get SBO date Format in Number"
    '********************************************************************
    'Type		            :   Public Procedure     
    'Name               	:	IntSBODateFormat
    'Parameter          	:   none
    'Return Value       	:	
    'Author             	:	Manu
    'Created Date       	:	
    'Last Modified By	    :	
    'Modified Date        	:	
    'Purpose             	:	To get date Format(integer value) as applicable to SBO
    '********************************************************************
    Public Function NumSBODateFormat() As String
        Try
            Dim rsDate As SAPbobsCOM.Recordset
            Dim strsql As String
            Dim DateSep As Char

            strsql = "Select DateFormat,DateSep from OADM"
            rsDate = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
            rsDate.DoQuery(strsql)
            DateSep = rsDate.Fields.Item(1).Value

            Select Case rsDate.Fields.Item(0).Value
                Case 0
                    NumSBODateFormat = 3
                Case 1
                    NumSBODateFormat = 103
                Case 2
                    NumSBODateFormat = 1
                Case 3
                    NumSBODateFormat = 120
                Case 4
                    NumSBODateFormat = 126
                Case 5
                    NumSBODateFormat = 130
            End Select
            Return NumSBODateFormat

        Catch ex As Exception
            Throw ex
        End Try
    End Function
#End Region

#End Region

#Region "Get Rental Period"
    Public Function getRentalDays(ByVal Date1 As String, ByVal Date2 As String, ByVal IsWeekDaysBilling As Boolean) As Integer
        Dim TotalDays, TotalDaysincSat, TotalBillableDays As Integer
        Dim TotalWeekEnds As Integer
        Dim StartDate As Date
        Dim EndDate As Date
        Dim oRecordset As SAPbobsCOM.Recordset

        StartDate = CType(Date1.Insert(4, "/").Insert(7, "/"), Date)
        EndDate = CType(Date2.Insert(4, "/").Insert(7, "/"), Date)

        TotalDays = DateDiff(DateInterval.Day, StartDate, EndDate)

        If IsWeekDaysBilling Then
            strSQL = " select dbo.WeekDays('" & Date1 & "','" & Date2 & "')"
            oApplication.Utilities.ExecuteSQL(oRecordset, strSQL)
            If oRecordset.RecordCount > 0 Then
                TotalBillableDays = oRecordset.Fields.Item(0).Value
            End If
            Return TotalBillableDays
        Else
            Return TotalDays + 1
        End If

    End Function

    Public Function WorkDays(ByVal dtBegin As Date, ByVal dtEnd As Date) As Long
        Try
            Dim dtFirstSunday As Date
            Dim dtLastSaturday As Date
            Dim lngWorkDays As Long

            ' get first sunday in range
            dtFirstSunday = dtBegin.AddDays((8 - Weekday(dtBegin)) Mod 7)

            ' get last saturday in range
            dtLastSaturday = dtEnd.AddDays(-(Weekday(dtEnd) Mod 7))

            ' get work days between first sunday and last saturday
            lngWorkDays = (((DateDiff(DateInterval.Day, dtFirstSunday, dtLastSaturday)) + 1) / 7) * 5

            ' if first sunday is not begin date
            If dtFirstSunday <> dtBegin Then

                ' assume first sunday is after begin date
                ' add workdays from begin date to first sunday
                lngWorkDays = lngWorkDays + (7 - Weekday(dtBegin))

            End If

            ' if last saturday is not end date
            If dtLastSaturday <> dtEnd Then

                ' assume last saturday is before end date
                ' add workdays from last saturday to end date
                lngWorkDays = lngWorkDays + (Weekday(dtEnd) - 1)

            End If

            WorkDays = lngWorkDays
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try


    End Function

#End Region

#Region "Get Item Price with Factor"
    Public Function getPrcWithFactor(ByVal CardCode As String, ByVal ItemCode As String, ByVal RntlDays As Integer, ByVal Qty As Double) As Double
        Dim oItem As SAPbobsCOM.Items
        Dim Price, Expressn As Double
        Dim oDataSet, oRecSet As SAPbobsCOM.Recordset

        oItem = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oItems)
        oApplication.Utilities.ExecuteSQL(oDataSet, "Select U_RentFac, U_NumDys From [@REN_FACT] order by U_NumDys ")
        If oItem.GetByKey(ItemCode) And oDataSet.RecordCount > 0 Then

            oApplication.Utilities.ExecuteSQL(oRecSet, "Select ListNum from OCRD where CardCode = '" & CardCode & "'")
            oItem.PriceList.SetCurrentLine(oRecSet.Fields.Item(0).Value - 1)
            Price = oItem.PriceList.Price
            Expressn = 0
            oDataSet.MoveFirst()

            While RntlDays > 0

                If oDataSet.EoF Then
                    oDataSet.MoveLast()
                End If

                If RntlDays < oDataSet.Fields.Item(1).Value Then
                    Expressn += (oDataSet.Fields.Item(0).Value * RntlDays * Price * Qty)
                    RntlDays = 0
                    Exit While
                End If
                Expressn += (oDataSet.Fields.Item(0).Value * oDataSet.Fields.Item(1).Value * Price * Qty)
                RntlDays -= oDataSet.Fields.Item(1).Value
                oDataSet.MoveNext()

            End While

        End If
        If oItem.UserFields.Fields.Item("U_Rental").Value = "Y" Then
            Return CDbl(Expressn / Qty)
        Else
            Return Price
        End If


    End Function
#End Region

#Region "Get WareHouse List"
    Public Function getUsedWareHousesList(ByVal ItemCode As String, ByVal Quantity As Double) As DataTable
        Dim oDataTable As DataTable
        Dim oRow As DataRow
        Dim rswhs As SAPbobsCOM.Recordset
        Dim LeftQty As Double
        Try
            oDataTable = New DataTable
            oDataTable.Columns.Add(New System.Data.DataColumn("ItemCode"))
            oDataTable.Columns.Add(New System.Data.DataColumn("WhsCode"))
            oDataTable.Columns.Add(New System.Data.DataColumn("Quantity"))

            strSQL = "Select WhsCode, ItemCode, (OnHand + OnOrder - IsCommited) As Available From OITW Where ItemCode = '" & ItemCode & "' And " & _
                        "WhsCode Not In (Select Whscode From OWHS Where U_Reserved = 'Y' Or U_Rental = 'Y') Order By (OnHand + OnOrder - IsCommited) Desc "

            ExecuteSQL(rswhs, strSQL)
            LeftQty = Quantity

            While Not rswhs.EoF
                oRow = oDataTable.NewRow()

                oRow.Item("WhsCode") = rswhs.Fields.Item("WhsCode").Value
                oRow.Item("ItemCode") = rswhs.Fields.Item("ItemCode").Value

                LeftQty = LeftQty - CType(rswhs.Fields.Item("Available").Value, Double)

                If LeftQty <= 0 Then
                    oRow.Item("Quantity") = CType(rswhs.Fields.Item("Available").Value, Double) + LeftQty
                    oDataTable.Rows.Add(oRow)
                    Exit While
                Else
                    oRow.Item("Quantity") = CType(rswhs.Fields.Item("Available").Value, Double)
                End If

                oDataTable.Rows.Add(oRow)
                rswhs.MoveNext()
                oRow = Nothing
            End While

            'strSQL = ""
            'For count As Integer = 0 To oDataTable.Rows.Count - 1
            '    strSQL += oDataTable.Rows(count).Item("WhsCode") & " : " & oDataTable.Rows(count).Item("Quantity") & vbNewLine
            'Next
            'MessageBox.Show(strSQL)

            Return oDataTable

        Catch ex As Exception
            Throw ex
        Finally
            oRow = Nothing
        End Try
    End Function
#End Region

#End Region

    Public Function validateAuthorization(ByVal aUserId As String, ByVal aFormUID As String) As Boolean
        Dim oAuth As SAPbobsCOM.Recordset
        oAuth = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
        Dim struserid As String
        '    Return False
        struserid = oApplication.Company.UserName
        oAuth.DoQuery("select * from UPT1 where ""FormId""='" & aFormUID & "'")
        If (oAuth.RecordCount <= 0) Then
            Return True
        Else
            Dim st As String
            st = oAuth.Fields.Item("PermId").Value
            st = "Select * from USR3 where ""PermId""='" & st & "' and ""UserLink""=" & aUserId
            oAuth.DoQuery(st)
            If oAuth.RecordCount > 0 Then
                If oAuth.Fields.Item("Permission").Value = "N" Then
                    Return False
                End If
                Return True
            Else
                Return True
            End If

        End If

        Return True

    End Function
    Public Sub assignMatrixLineno(ByVal aGrid As SAPbouiCOM.Grid, ByVal aform As SAPbouiCOM.Form)
        aform.Freeze(True)
        Try
            For intNo As Integer = 0 To aGrid.DataTable.Rows.Count - 1
                aGrid.RowHeaders.SetText(intNo, intNo + 1)
            Next
        Catch ex As Exception
        End Try
        aGrid.Columns.Item("RowsHeader").TitleObject.Caption = "#"
        aform.Freeze(False)
    End Sub

#Region "Functions related to Load XML"

#Region "Add/Remove Menus "
    Public Sub AddRemoveMenus(ByVal sFileName As String)
        Dim oXMLDoc As New Xml.XmlDocument
        Dim sFilePath As String
        Try
            sFilePath = getApplicationPath() & "\XML Files\" & sFileName
            oXMLDoc.Load(sFilePath)
            oApplication.SBO_Application.LoadBatchActions(oXMLDoc.InnerXml)
        Catch ex As Exception
            Throw ex
        Finally
            oXMLDoc = Nothing
        End Try
    End Sub
#End Region

#Region "Load XML File "
    Private Function LoadXMLFiles(ByVal sFileName As String) As String
        Dim oXmlDoc As Xml.XmlDocument
        Dim oXNode As Xml.XmlNode
        Dim oAttr As Xml.XmlAttribute
        Dim sPath As String
        Dim FrmUID As String
        Try
            oXmlDoc = New Xml.XmlDocument

            sPath = getApplicationPath() & "\XML Files\" & sFileName

            oXmlDoc.Load(sPath)
            oXNode = oXmlDoc.GetElementsByTagName("form").Item(0)
            oAttr = oXNode.Attributes.GetNamedItem("uid")
            oAttr.Value = oAttr.Value & FormNum
            FormNum = FormNum + 1
            oApplication.SBO_Application.LoadBatchActions(oXmlDoc.InnerXml)
            FrmUID = oAttr.Value

            Return FrmUID

        Catch ex As Exception
            Throw ex
        Finally
            oXmlDoc = Nothing
        End Try
    End Function
#End Region

    Public Function LoadForm(ByVal XMLFile As String, ByVal FormType As String) As SAPbouiCOM.Form
        'Return LoadForm(XMLFile, FormType.ToString(), FormType & "_" & oApplication.SBO_Application.Forms.Count.ToString)
        LoadXMLFiles(XMLFile)
        Return Nothing
    End Function

    '*****************************************************************
    'Type               : Function   
    'Name               : LoadForm
    'Parameter          : XmlFile,FormType,FormUID
    'Return Value       : SBO Form
    'Author             : Senthil Kumar B Senthil Kumar B
    'Created Date       : 
    'Last Modified By   : 
    'Modified Date      : 
    'Purpose            : To Load XML file 
    '*****************************************************************

    Public Function LoadForm(ByVal XMLFile As String, ByVal FormType As String, ByVal FormUID As String) As SAPbouiCOM.Form

        Dim oXML As System.Xml.XmlDocument
        Dim objFormCreationParams As SAPbouiCOM.FormCreationParams
        Try
            oXML = New System.Xml.XmlDocument
            oXML.Load(XMLFile)
            objFormCreationParams = (oApplication.SBO_Application.CreateObject(SAPbouiCOM.BoCreatableObjectType.cot_FormCreationParams))
            objFormCreationParams.XmlData = oXML.InnerXml
            objFormCreationParams.FormType = FormType
            objFormCreationParams.UniqueID = FormUID
            Return oApplication.SBO_Application.Forms.AddEx(objFormCreationParams)
        Catch ex As Exception
            Throw ex

        End Try

    End Function



#Region "Load Forms"
    Public Sub LoadForm(ByRef oObject As Object, ByVal XmlFile As String)
        Try
            oObject.FrmUID = LoadXMLFiles(XmlFile)
            oObject.Form = oApplication.SBO_Application.Forms.Item(oObject.FrmUID)
            If Not oApplication.Collection.ContainsKey(oObject.FrmUID) Then
                oApplication.Collection.Add(oObject.FrmUID, oObject)
            End If
        Catch ex As Exception
            Throw ex
        End Try
    End Sub
#End Region

#End Region
    Public Sub setEdittextvalue(ByVal aform As SAPbouiCOM.Form, ByVal UID As String, ByVal newvalue As String)
        Dim objEdit As SAPbouiCOM.EditText
        objEdit = aform.Items.Item(UID).Specific
        objEdit.String = newvalue
    End Sub
#Region "Functions related to System Initilization"

#Region "Create Tables"
    Public Sub CreateTables()
        Dim oCreateTable As clsTable
        Try
            oCreateTable = New clsTable
            oCreateTable.CreateTables()
        Catch ex As Exception
            Throw ex
        Finally
            oCreateTable = Nothing
        End Try
    End Sub
#End Region

#Region "Notify Alert"
    Public Sub NotifyAlert()
        'Dim oAlert As clsPromptAlert

        'Try
        '    oAlert = New clsPromptAlert
        '    oAlert.AlertforEndingOrdr()
        'Catch ex As Exception
        '    Throw ex
        'Finally
        '    oAlert = Nothing
        'End Try
    End Sub
#End Region

#End Region

#Region "Function related to Quantities"

#Region "Get Available Quantity"
    Public Function getAvailableQty(ByVal ItemCode As String) As Long
        Dim rsQuantity As SAPbobsCOM.Recordset

        strSQL = "Select SUM(T1.OnHand + T1.OnOrder - T1.IsCommited) From OITW T1 Left Outer Join OWHS T3 On T3.Whscode = T1.WhsCode " & _
                    "Where T1.ItemCode = '" & ItemCode & "'"
        Me.ExecuteSQL(rsQuantity, strSQL)

        If rsQuantity.Fields.Item(0) Is System.DBNull.Value Then
            Return 0
        Else
            Return CLng(rsQuantity.Fields.Item(0).Value)
        End If

    End Function
#End Region

#Region "Get Rented Quantity"
    Public Function getRentedQty(ByVal ItemCode As String, ByVal StartDate As String, ByVal EndDate As String) As Long
        Dim rsQuantity As SAPbobsCOM.Recordset
        Dim RentedQty As Long

        strSQL = " select Sum(U_ReqdQty) from [@REN_RDR1] Where U_ItemCode = '" & ItemCode & "' " & _
                    " And DocEntry IN " & _
                    " (Select DocEntry from [@REN_ORDR] Where U_Status = 'R') " & _
                    " and '" & StartDate & "' between [@REN_RDR1].U_ShipDt1 and [@REN_RDR1].U_ShipDt2 "
        '" and [@REN_RDR1].U_ShipDt1 between '" & StartDate & "' and '" & EndDate & "'"

        ExecuteSQL(rsQuantity, strSQL)
        If Not rsQuantity.Fields.Item(0).Value Is System.DBNull.Value Then
            RentedQty = rsQuantity.Fields.Item(0).Value
        End If

        Return RentedQty

    End Function
#End Region

#Region "Get Reserved Quantity"
    Public Function getReservedQty(ByVal ItemCode As String, ByVal StartDate As String, ByVal EndDate As String) As Long
        Dim rsQuantity As SAPbobsCOM.Recordset
        Dim ReservedQty As Long

        strSQL = " select Sum(U_ReqdQty) from [@REN_QUT1] Where U_ItemCode = '" & ItemCode & "' " & _
                    " And DocEntry IN " & _
                    " (Select DocEntry from [@REN_OQUT] Where U_Status = 'R' And Status = 'O') " & _
                    " and '" & StartDate & "' between [@REN_QUT1].U_ShipDt1 and [@REN_QUT1].U_ShipDt2"

        ExecuteSQL(rsQuantity, strSQL)
        If Not rsQuantity.Fields.Item(0).Value Is System.DBNull.Value Then
            ReservedQty = rsQuantity.Fields.Item(0).Value
        End If

        Return ReservedQty

    End Function
#End Region

#End Region

#Region "Functions related to Tax"

#Region "Get Tax Codes"
    Public Sub getTaxCodes(ByRef oCombo As SAPbouiCOM.ComboBox)
        Dim rsTaxCodes As SAPbobsCOM.Recordset

        strSQL = "Select Code, Name From OVTG Where Category = 'O' Order By Name"
        Me.ExecuteSQL(rsTaxCodes, strSQL)

        oCombo.ValidValues.Add("", "")
        If rsTaxCodes.RecordCount > 0 Then
            While Not rsTaxCodes.EoF
                oCombo.ValidValues.Add(rsTaxCodes.Fields.Item(0).Value, rsTaxCodes.Fields.Item(1).Value)
                rsTaxCodes.MoveNext()
            End While
        End If
        oCombo.ValidValues.Add("Define New", "Define New")
        'oCombo.Select("")
    End Sub
#End Region

#Region "Get Applicable Code"

    Public Function getApplicableTaxCode1(ByVal CardCode As String, ByVal ItemCode As String, ByVal Shipto As String) As String
        Dim oBP As SAPbobsCOM.BusinessPartners
        Dim oItem As SAPbobsCOM.Items
        Dim rsExempt As SAPbobsCOM.Recordset
        Dim TaxGroup As String
        oBP = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oBusinessPartners)

        If oBP.GetByKey(CardCode.Trim) Then
            If oBP.VatLiable = SAPbobsCOM.BoVatStatus.vLiable Or oBP.VatLiable = SAPbobsCOM.BoVatStatus.vEC Then
                If oBP.VatGroup.Trim <> "" Then
                    TaxGroup = oBP.VatGroup.Trim
                Else
                    strSQL = "select LicTradNum from CRD1 where Address ='" & Shipto & "' and CardCode ='" & CardCode & "'"
                    Me.ExecuteSQL(rsExempt, strSQL)
                    If rsExempt.RecordCount > 0 Then
                        rsExempt.MoveFirst()
                        TaxGroup = rsExempt.Fields.Item(0).Value
                    Else
                        TaxGroup = ""
                    End If
                    'TaxGroup = oBP.FederalTaxID
                End If
            ElseIf oBP.VatLiable = SAPbobsCOM.BoVatStatus.vExempted Then
                strSQL = "Select Code From OVTG Where Rate = 0 And Category = 'O' Order By Code"
                Me.ExecuteSQL(rsExempt, strSQL)
                If rsExempt.RecordCount > 0 Then
                    rsExempt.MoveFirst()
                    TaxGroup = rsExempt.Fields.Item(0).Value
                Else
                    TaxGroup = ""
                End If
            End If
        End If




        Return TaxGroup

    End Function


    Public Function getApplicableTaxCode(ByVal CardCode As String, ByVal ItemCode As String) As String
        Dim oBP As SAPbobsCOM.BusinessPartners
        Dim oItem As SAPbobsCOM.Items
        Dim rsExempt As SAPbobsCOM.Recordset
        Dim TaxGroup As String
        oBP = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oBusinessPartners)

        If oBP.GetByKey(CardCode.Trim) Then
            If oBP.VatLiable = SAPbobsCOM.BoVatStatus.vLiable Or oBP.VatLiable = SAPbobsCOM.BoVatStatus.vEC Then
                If oBP.VatGroup.Trim <> "" Then
                    TaxGroup = oBP.VatGroup.Trim
                Else
                    TaxGroup = oBP.FederalTaxID
                End If
            ElseIf oBP.VatLiable = SAPbobsCOM.BoVatStatus.vExempted Then
                strSQL = "Select Code From OVTG Where Rate = 0 And Category = 'O' Order By Code"
                Me.ExecuteSQL(rsExempt, strSQL)
                If rsExempt.RecordCount > 0 Then
                    rsExempt.MoveFirst()
                    TaxGroup = rsExempt.Fields.Item(0).Value
                Else
                    TaxGroup = ""
                End If
            End If
        End If

        'If oBP.GetByKey(CardCode.Trim) Then
        '    If oBP.VatLiable = SAPbobsCOM.BoVatStatus.vLiable Or oBP.VatLiable = SAPbobsCOM.BoVatStatus.vEC Then
        '        If oBP.VatGroup.Trim <> "" Then
        '            TaxGroup = oBP.VatGroup.Trim
        '        Else
        '            oItem = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oItems)
        '            If oItem.GetByKey(ItemCode.Trim) Then
        '                TaxGroup = oItem.SalesVATGroup.Trim
        '            End If
        '        End If
        '    ElseIf oBP.VatLiable = SAPbobsCOM.BoVatStatus.vExempted Then
        '        strSQL = "Select Code From OVTG Where Rate = 0 And Category = 'O' Order By Code"
        '        Me.ExecuteSQL(rsExempt, strSQL)
        '        If rsExempt.RecordCount > 0 Then
        '            rsExempt.MoveFirst()
        '            TaxGroup = rsExempt.Fields.Item(0).Value
        '        Else
        '            TaxGroup = ""
        '        End If
        '    End If
        'End If
        Return TaxGroup

    End Function
#End Region

#End Region

#Region "Log Transaction"
    Public Sub LogTransaction(ByVal DocNum As Integer, ByVal ItemCode As String, _
                                    ByVal FromWhs As String, ByVal TransferedQty As Double, ByVal ProcessDate As Date)
        Dim sCode As String
        Dim sColumns As String
        Dim sValues As String
        Dim rsInsert As SAPbobsCOM.Recordset

        sCode = Me.getMaxCode("@REN_PORDR", "Code")

        sColumns = "Code, Name, U_DocNum, U_WhsCode, U_ItemCode, U_Quantity, U_RetQty, U_Date"
        sValues = "'" & sCode & "','" & sCode & "'," & DocNum & ",'" & FromWhs & "','" & ItemCode & "'," & TransferedQty & ", 0, Convert(DateTime,'" & ProcessDate.ToString("yyyyMMdd") & "')"

        strSQL = "Insert into [@REN_PORDR] (" & sColumns & ") Values (" & sValues & ")"
        oApplication.Utilities.ExecuteSQL(rsInsert, strSQL)

    End Sub

    Public Sub LogCreatedDocument(ByVal DocNum As Integer, ByVal CreatedDocType As SAPbouiCOM.BoLinkedObject, ByVal CreatedDocNum As String, ByVal sCreatedDate As String)
        Dim oUserTable As SAPbobsCOM.UserTable
        Dim sCode As String
        Dim CreatedDate As DateTime
        Try
            oUserTable = oApplication.Company.UserTables.Item("REN_DORDR")

            sCode = Me.getMaxCode("@REN_DORDR", "Code")

            If Not oUserTable.GetByKey(sCode) Then
                oUserTable.Code = sCode
                oUserTable.Name = sCode

                With oUserTable.UserFields.Fields
                    .Item("U_DocNum").Value = DocNum
                    .Item("U_DocType").Value = CInt(CreatedDocType)
                    .Item("U_DocEntry").Value = CInt(CreatedDocNum)

                    If sCreatedDate <> "" Then
                        CreatedDate = CDate(sCreatedDate.Insert(4, "/").Insert(7, "/"))
                        .Item("U_Date").Value = CreatedDate
                    Else
                        .Item("U_Date").Value = CDate(Format(Now, "Long Date"))
                    End If

                End With

                If oUserTable.Add <> 0 Then
                    Throw New Exception(oApplication.Company.GetLastErrorDescription)
                End If
            End If

        Catch ex As Exception
            Throw ex
        Finally
            oUserTable = Nothing
        End Try
    End Sub
#End Region

    Public Function getLocalCurrency(ByVal strCurrency As String) As Double
        Dim oTemp As SAPbobsCOM.Recordset
        oTemp = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
        oTemp.DoQuery("Select Maincurrncy from OADM")
        Return oTemp.Fields.Item(0).Value
    End Function

#Region "Get ExchangeRate"
    Public Function getExchangeRate(ByVal strCurrency As String) As Double
        Dim oTemp As SAPbobsCOM.Recordset
        Dim dblExchange As Double
        If GetCurrency("Local") = strCurrency Then
            dblExchange = 1
        Else
            oTemp = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
            oTemp.DoQuery("Select IFNULL(Rate,0) from ORTT where convert(nvarchar(10),RateDate,101)=Convert(nvarchar(10),getdate(),101) and currency='" & strCurrency & "'")
            dblExchange = oTemp.Fields.Item(0).Value
        End If
        Return dblExchange
    End Function

    Public Function getExchangeRate(ByVal strCurrency As String, ByVal dtdate As Date) As Double
        Dim oTemp As SAPbobsCOM.Recordset
        Dim strSql As String
        Dim dblExchange As Double
        If GetCurrency("Local") = strCurrency Then
            dblExchange = 1
        Else
            oTemp = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
            strSql = "Select IFNULL(Rate,0) from ORTT where ratedate='" & dtdate.ToString("yyyy-MM-dd") & "' and currency='" & strCurrency & "'"
            oTemp.DoQuery(strSql)
            dblExchange = oTemp.Fields.Item(0).Value
        End If
        Return dblExchange
    End Function
#End Region

    Public Function GetDateTimeValue(ByVal DateString As String) As DateTime
        Dim objBridge As SAPbobsCOM.SBObob
        objBridge = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoBridge)
        Return objBridge.Format_StringToDate(DateString).Fields.Item(0).Value
    End Function

#Region "Get DocCurrency"
    Public Function GetDocCurrency(ByVal aDocEntry As Integer) As String
        Dim oTemp As SAPbobsCOM.Recordset
        oTemp = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
        oTemp.DoQuery("Select DocCur from OINV where docentry=" & aDocEntry)
        Return oTemp.Fields.Item(0).Value
    End Function
#End Region

#Region "GetEditTextValues"
    Public Function getEditTextvalue(ByVal aForm As SAPbouiCOM.Form, ByVal strUID As String) As String
        Dim oEditText As SAPbouiCOM.EditText
        oEditText = aForm.Items.Item(strUID).Specific
        Return oEditText.Value
    End Function
#End Region

#Region "Get Currency"
    Public Function GetCurrency(ByVal strChoice As String, Optional ByVal aCardCode As String = "") As String
        Dim strCurrQuery, Currency As String
        Dim oTempCurrency As SAPbobsCOM.Recordset
        If strChoice = "Local" Then
            strCurrQuery = "Select MainCurncy from OADM"
        Else
            strCurrQuery = "Select Currency from OCRD where CardCode='" & aCardCode & "'"
        End If
        oTempCurrency = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
        oTempCurrency.DoQuery(strCurrQuery)
        Currency = oTempCurrency.Fields.Item(0).Value
        Return Currency
    End Function

#End Region

    Public Function FormatDataSourceValue(ByVal Value As String) As Double
        Dim NewValue As Double

        If Value <> "" Then
            If Value.IndexOf(".") > -1 Then
                Value = Value.Replace(".", CompanyDecimalSeprator)
            End If

            If Value.IndexOf(CompanyThousandSeprator) > -1 Then
                Value = Value.Replace(CompanyThousandSeprator, "")
            End If
        Else
            Value = "0"

        End If

        ' NewValue = CDbl(Value)
        NewValue = Val(Value)

        Return NewValue


        'Dim dblValue As Double
        'Value = Value.Replace(CompanyThousandSeprator, "")
        'Value = Value.Replace(CompanyDecimalSeprator, System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
        'dblValue = Val(Value)
        'Return dblValue
    End Function

    Public Function FormatScreenValues(ByVal Value As String) As Double
        Dim NewValue As Double

        If Value <> "" Then
            If Value.IndexOf(".") > -1 Then
                Value = Value.Replace(".", CompanyDecimalSeprator)
            End If
        Else
            Value = "0"
        End If

        'NewValue = CDbl(Value)
        NewValue = Val(Value)

        Return NewValue

        'Dim dblValue As Double
        'Value = Value.Replace(CompanyThousandSeprator, "")
        'Value = Value.Replace(CompanyDecimalSeprator, System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
        'dblValue = Val(Value)
        'Return dblValue

    End Function

    Public Function SetScreenValues(ByVal Value As String) As String

        If Value.IndexOf(CompanyDecimalSeprator) > -1 Then
            Value = Value.Replace(CompanyDecimalSeprator, ".")
        End If

        Return Value

    End Function

    Public Function SetDBValues(ByVal Value As String) As String

        If Value.IndexOf(CompanyDecimalSeprator) > -1 Then
            Value = Value.Replace(CompanyDecimalSeprator, ".")
        End If

        Return Value

    End Function

    Public Sub OpenFileDialogBox(ByVal oForm As SAPbouiCOM.Form, ByVal strID As String)
        Dim _retVal As String = String.Empty
        Try
            FolderOpen()
            CType(oForm.Items.Item(strID).Specific, SAPbouiCOM.EditText).Value = strFilepath
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Private Sub FolderOpen()
        Try
            Dim mythr As New System.Threading.Thread(AddressOf ShowFileDialog)
            mythr.SetApartmentState(Threading.ApartmentState.STA)
            mythr.Start()
            mythr.Join()
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Private Sub ShowFileDialog()
        Try

            Dim oDialogBox As New FolderBrowserDialog
            Dim strMdbFilePath As String
            Dim oProcesses() As Process


            Try
                oProcesses = Process.GetProcessesByName("SAP Business One")
                If oProcesses.Length <> 0 Then
                    If strFilterFile.ToString() <> "" Then
                        oDialogBox.RootFolder = strFilterFile
                    End If
                    For i As Integer = 0 To oProcesses.Length - 1
                        Dim MyWindow As New clsListener.WindowWrapper(oProcesses(i).MainWindowHandle)
                        If oDialogBox.ShowDialog(MyWindow) = DialogResult.OK Then
                            strMdbFilePath = oDialogBox.SelectedPath
                            strFilepath = oDialogBox.SelectedPath
                        Else
                        End If
                    Next
                End If
            Catch ex As Exception
                oApplication.Utilities.Message(ex.Message, SAPbouiCOM.BoStatusBarMessageType.smt_Error)
            Finally
            End Try
        Catch ex As Exception
            Throw ex
        End Try
    End Sub
#Region "AddControls"
    Public Sub AddControls(ByVal objForm As SAPbouiCOM.Form, ByVal ItemUID As String, ByVal SourceUID As String, ByVal ItemType As SAPbouiCOM.BoFormItemTypes, ByVal position As String, Optional ByVal fromPane As Integer = 1, Optional ByVal toPane As Integer = 1, Optional ByVal linkedUID As String = "", Optional ByVal strCaption As String = "", Optional ByVal dblWidth As Double = 0, Optional ByVal dblTop As Double = 0, Optional ByVal Hight As Double = 0, Optional ByVal Enable As Boolean = True)
        Dim objNewItem, objOldItem As SAPbouiCOM.Item
        Dim ostatic As SAPbouiCOM.StaticText
        Dim oButton As SAPbouiCOM.Button
        Dim oCheckbox As SAPbouiCOM.CheckBox
        Dim oEditText As SAPbouiCOM.EditText
        Dim ofolder As SAPbouiCOM.Folder
        objOldItem = objForm.Items.Item(SourceUID)
        objNewItem = objForm.Items.Add(ItemUID, ItemType)
        With objNewItem
            If ItemType = SAPbouiCOM.BoFormItemTypes.it_LINKED_BUTTON Then
                .Left = objOldItem.Left - 15
                .Top = objOldItem.Top + 1
                .LinkTo = linkedUID
            Else
                If position.ToUpper = "RIGHT" Then
                    .Left = objOldItem.Left + objOldItem.Width + 5
                    .Top = objOldItem.Top
                ElseIf position.ToUpper = "DOWN" Then
                    If ItemUID = "edWork" Then
                        .Left = objOldItem.Left + 40
                    Else
                        .Left = objOldItem.Left
                    End If
                    .Top = objOldItem.Top + objOldItem.Height + 3

                    .Width = objOldItem.Width
                    .Height = objOldItem.Height
                ElseIf position.ToUpper = "COPY" Then
                    .Top = objOldItem.Top
                    .Left = objOldItem.Left
                    .Height = objOldItem.Height
                    .Width = objOldItem.Width
                End If
            End If
            .FromPane = fromPane
            .ToPane = toPane
            If ItemType = SAPbouiCOM.BoFormItemTypes.it_STATIC Then
                .LinkTo = linkedUID
            End If
            .LinkTo = linkedUID
        End With
        If (ItemType = SAPbouiCOM.BoFormItemTypes.it_EDIT Or ItemType = SAPbouiCOM.BoFormItemTypes.it_STATIC) Then
            objNewItem.Width = objOldItem.Width
        End If
        If ItemType = SAPbouiCOM.BoFormItemTypes.it_BUTTON Then
            objNewItem.Width = objOldItem.Width '+ 50
            oButton = objNewItem.Specific
            oButton.Caption = strCaption
        ElseIf ItemType = SAPbouiCOM.BoFormItemTypes.it_FOLDER Then
            ofolder = objNewItem.Specific
            ofolder.Caption = strCaption
            ofolder.GroupWith(linkedUID)
        ElseIf ItemType = SAPbouiCOM.BoFormItemTypes.it_STATIC Then
            ostatic = objNewItem.Specific
            ostatic.Caption = strCaption
        ElseIf ItemType = SAPbouiCOM.BoFormItemTypes.it_CHECK_BOX Then
            oCheckbox = objNewItem.Specific
            oCheckbox.Caption = strCaption

        End If
        If dblWidth <> 0 Then
            objNewItem.Width = dblWidth
        End If

        If dblTop <> 0 Then
            objNewItem.Top = objNewItem.Top + dblTop
        End If
        If Hight <> 0 Then
            objNewItem.Height = objNewItem.Height + Hight
        End If
    End Sub
#End Region

#Region "Set / Get Values from Matrix"
    Public Function getMatrixValues(ByVal aMatrix As SAPbouiCOM.Matrix, ByVal coluid As String, ByVal intRow As Integer) As String
        Return aMatrix.Columns.Item(coluid).Cells.Item(intRow).Specific.value
    End Function
    Public Sub SetMatrixValues(ByVal aMatrix As SAPbouiCOM.Matrix, ByVal coluid As String, ByVal intRow As Integer, ByVal strvalue As String)
        aMatrix.Columns.Item(coluid).Cells.Item(intRow).Specific.value = strvalue
    End Sub
#End Region

#Region "Add Condition CFL"
    Public Sub AddConditionCFL(ByVal FormUID As String, ByVal strQuery As String, ByVal strQueryField As String, ByVal sCFL As String)
        Dim oRecordSet As SAPbobsCOM.Recordset
        Dim oCFLs As SAPbouiCOM.ChooseFromListCollection
        Dim Conditions As SAPbouiCOM.Conditions
        Dim oCond As SAPbouiCOM.Condition
        Dim oCFL As SAPbouiCOM.ChooseFromList
        Dim oCFLCreationParams As SAPbouiCOM.ChooseFromListCreationParams
        Dim sDocEntry As New ArrayList()
        Dim sDocNum As ArrayList
        Dim MatrixItem As ArrayList
        sDocEntry = New ArrayList()
        sDocNum = New ArrayList()
        MatrixItem = New ArrayList()

        Try
            oCFLs = oApplication.SBO_Application.Forms.Item(FormUID).ChooseFromLists
            oCFLCreationParams = oApplication.SBO_Application.CreateObject( _
                                    SAPbouiCOM.BoCreatableObjectType.cot_ChooseFromListCreationParams)

            oCFL = oCFLs.Item(sCFL)

            Dim oRec As SAPbobsCOM.Recordset
            oRec = DirectCast(oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset), SAPbobsCOM.Recordset)
            oRec.DoQuery(strQuery)
            oRec.MoveFirst()

            Try
                If oRec.EoF Then
                    sDocEntry.Add("")
                Else
                    While Not oRec.EoF
                        Dim DocNum As String = oRec.Fields.Item(strQueryField).Value.ToString()
                        If DocNum <> "" Then
                            sDocEntry.Add(DocNum)
                        End If
                        oRec.MoveNext()
                    End While
                End If
            Catch generatedExceptionName As Exception
                Throw
            End Try

            'If IsMatrixCondition = True Then
            '    Dim oMatrix As SAPbouiCOM.Matrix
            '    oMatrix = DirectCast(oForm.Items.Item(Matrixname).Specific, SAPbouiCOM.Matrix)

            '    For a As Integer = 1 To oMatrix.RowCount
            '        If a <> pVal.Row Then
            '            MatrixItem.Add(DirectCast(oMatrix.Columns.Item(columnname).Cells.Item(a).Specific, SAPbouiCOM.EditText).Value)
            '        End If
            '    Next
            '    If removelist = True Then
            '        For xx As Integer = 0 To MatrixItem.Count - 1
            '            Dim zz As String = MatrixItem(xx).ToString()
            '            If sDocEntry.Contains(zz) Then
            '                sDocEntry.Remove(zz)
            '            End If
            '        Next
            '    End If
            'End If

            'oCFLs = oForm.ChooseFromLists
            'oCFLCreationParams = DirectCast(SBO_Application.CreateObject(SAPbouiCOM.BoCreatableObjectType.cot_ChooseFromListCreationParams), SAPbouiCOM.ChooseFromListCreationParams)
            'If systemMatrix = True Then
            '    Dim oCFLEvento As SAPbouiCOM.IChooseFromListEvent = Nothing
            '    oCFLEvento = DirectCast(pVal, SAPbouiCOM.IChooseFromListEvent)
            '    Dim sCFL_ID As String = Nothing
            '    sCFL_ID = oCFLEvento.ChooseFromListUID
            '    oCFL = oForm.ChooseFromLists.Item(sCFL_ID)
            'Else
            '    oCFL = oForm.ChooseFromLists.Item(sCHUD)
            'End If

            Conditions = New SAPbouiCOM.Conditions()
            oCFL.SetConditions(Conditions)
            Conditions = oCFL.GetConditions()
            oCond = Conditions.Add()
            oCond.BracketOpenNum = 2
            For i As Integer = 0 To sDocEntry.Count - 1
                If i > 0 Then
                    oCond.Relationship = SAPbouiCOM.BoConditionRelationship.cr_OR
                    oCond = Conditions.Add()
                    oCond.BracketOpenNum = 1
                End If

                oCond.[Alias] = strQueryField
                oCond.Operation = SAPbouiCOM.BoConditionOperation.co_EQUAL
                oCond.CondVal = sDocEntry(i).ToString()
                If i + 1 = sDocEntry.Count Then
                    oCond.BracketCloseNum = 2
                Else
                    oCond.BracketCloseNum = 1
                End If
            Next

            oCFL.SetConditions(Conditions)


        Catch ex As Exception
            Throw ex
        End Try
    End Sub
#End Region

    Public Sub SendMessage(ByVal strMobileNo As String, ByVal strMsg As String)
        Try
            Dim nvCollection As New NameValueCollection
            Dim oRequest As New Net.WebClient
            Dim responseArray() As Byte
            Dim strRequest As String = "http://www.smstoalert.com/Api/apisend/uid/caparsms/pwd/techo123/senderid/CAPAROLPAINTS/to/{0}/msg/{1}"
            Dim strMessage As String = String.Format(strRequest, strMobileNo, strMsg)
            MessageBox.Show(strMessage)
            'responseArray = oRequest.UploadValues(strRequest, "POST", nvCollection)
            'MessageBox.Show(Encoding.ASCII.GetString(responseArray))
        Catch ex As Exception

        End Try
    End Sub

    Public Sub addUDODefaultValues()
        Try
            Dim oGenService As SAPbobsCOM.GeneralService
            Dim oGenData As SAPbobsCOM.GeneralData
            Dim oGenDataCollection As SAPbobsCOM.GeneralDataCollection
            Dim oCompService As SAPbobsCOM.CompanyService
            Dim oChildData1 As SAPbobsCOM.GeneralData
            Dim oGeneralDataParams As SAPbobsCOM.GeneralDataParams
            Dim oRecordSet As SAPbobsCOM.Recordset
            oRecordSet = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)


            Dim oHashTable As New Hashtable
            oHashTable.Add("Type", "TypeDeTransaction")
            oHashTable.Add("DocNum", "NumeroDuDocument")
            oHashTable.Add("DocDate", "DateDuDocument")
            oHashTable.Add("ItemCode ", "CodeArticle")
            oHashTable.Add("Dscription ", "DescriptionArticle")
            oHashTable.Add("unitMsr", "unitesDinventaireDeMesure")
            oHashTable.Add("Quantity", "Quantite")
            oHashTable.Add("WhsCode", "CodeDentrepot")
            oHashTable.Add("Lot", "Lot")
            oHashTable.Add("BatchNum", "NumeroDeLot")
            'oHashTable.Add("unitMsr", "unitesDinventaireDeMesure")
            oHashTable.Add("ExpDate ", "DateDexpiration")
            oHashTable.Add("BQuantity ", "QtyParLot")
            oHashTable.Add("Documents ", "Racines")

            oCompService = oApplication.Company.GetCompanyService
            oGenService = oCompService.GetGeneralService("Z_HDF_OBND")
            oGenData = oGenService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralData)
            oGeneralDataParams = oGenService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralDataParams)


            Dim strQuery As String = "Select * From [@Z_HDF_OBND] Where U_Type = 'GRPO'"
            oRecordSet.DoQuery(strQuery)
            If oRecordSet.EoF Then
                strQuery = "Select * From [@Z_HDF_OBND]"
                oRecordSet.DoQuery(strQuery)
                Dim intRecord As Integer = oRecordSet.RecordCount + 1
                oGenData.SetProperty("Code", intRecord.ToString)
                oGenData.SetProperty("Name", intRecord.ToString)
                oGenData.SetProperty("U_Type", "GRPO")
                oGenDataCollection = oGenData.Child("Z_HDF_OBD1")
                For Each element As DictionaryEntry In oHashTable
                    oChildData1 = oGenDataCollection.Add()
                    oChildData1.SetProperty("U_EName", element.Key)
                    oChildData1.SetProperty("U_FName", element.Value)
                Next
                oGeneralDataParams = oGenService.Add(oGenData)
            End If

            oGenData = oGenService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralData)
            strQuery = "Select * From [@Z_HDF_OBND] Where U_Type = 'GR'"
            oRecordSet.DoQuery(strQuery)
            If oRecordSet.EoF Then
                strQuery = "Select * From [@Z_HDF_OBND]"
                oRecordSet.DoQuery(strQuery)
                Dim intRecord As Integer = oRecordSet.RecordCount + 1
                oGenData.SetProperty("Code", intRecord.ToString)
                oGenData.SetProperty("Name", intRecord.ToString)
                oGenData.SetProperty("U_Type", "GR")
                oGenDataCollection = oGenData.Child("Z_HDF_OBD1")
                For Each element As DictionaryEntry In oHashTable
                    oChildData1 = oGenDataCollection.Add()
                    oChildData1.SetProperty("U_EName", element.Key)
                    oChildData1.SetProperty("U_FName", element.Value)
                Next
                oGeneralDataParams = oGenService.Add(oGenData)
            End If

            oGenData = oGenService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralData)
            strQuery = "Select * From [@Z_HDF_OBND] Where U_Type = 'APCreditMemo'"
            oRecordSet.DoQuery(strQuery)
            If oRecordSet.EoF Then
                strQuery = "Select * From [@Z_HDF_OBND]"
                oRecordSet.DoQuery(strQuery)
                Dim intRecord As Integer = oRecordSet.RecordCount + 1
                oGenData.SetProperty("Code", intRecord.ToString)
                oGenData.SetProperty("Name", intRecord.ToString)
                oGenData.SetProperty("U_Type", "APCreditMemo")
                oGenDataCollection = oGenData.Child("Z_HDF_OBD1")
                For Each element As DictionaryEntry In oHashTable
                    oChildData1 = oGenDataCollection.Add()
                    oChildData1.SetProperty("U_EName", element.Key)
                    oChildData1.SetProperty("U_FName", element.Value)
                Next
                oGeneralDataParams = oGenService.Add(oGenData)
            End If

            oGenData = oGenService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralData)
            strQuery = "Select * From [@Z_HDF_OBND] Where U_Type = 'Inventory'"
            oRecordSet.DoQuery(strQuery)
            If oRecordSet.EoF Then
                strQuery = "Select * From [@Z_HDF_OBND]"
                oRecordSet.DoQuery(strQuery)
                Dim intRecord As Integer = oRecordSet.RecordCount + 1
                oGenData.SetProperty("Code", intRecord.ToString)
                oGenData.SetProperty("Name", intRecord.ToString)
                oGenData.SetProperty("U_Type", "Inventory")
                oGenDataCollection = oGenData.Child("Z_HDF_OBD1")
                For Each element As DictionaryEntry In oHashTable
                    oChildData1 = oGenDataCollection.Add()
                    oChildData1.SetProperty("U_EName", element.Key)
                    oChildData1.SetProperty("U_FName", element.Value)
                Next
                oGeneralDataParams = oGenService.Add(oGenData)
            End If

        Catch ex As Exception

        End Try
    End Sub

End Class
