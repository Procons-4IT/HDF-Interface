Imports System.Data.SqlClient
Imports System.Configuration
Imports System.IO
Imports System.Xml
Imports System.Text

Public Class ClsOutBound
    Inherits clsBase

    Dim oDBDataSource As SAPbouiCOM.DBDataSource
    Dim oDBDataSource_1 As SAPbouiCOM.DBDataSource
    Private oOrderGrid As SAPbouiCOM.Grid
    Private oSuccessGrid As SAPbouiCOM.Grid
    Private oFailureGrid As SAPbouiCOM.Grid
    Private oEditTextColumn As SAPbouiCOM.EditTextColumn
    Private oComoColumn As SAPbouiCOM.ComboBoxColumn
    Private oDTSuccess As SAPbouiCOM.DataTable
    Private oDTFailure As SAPbouiCOM.DataTable
    Private oCombo As SAPbouiCOM.ComboBox
    Dim strqry As String
    Dim ds As DataSet
    Dim oDetailDt As DataTable
    Dim strQuery As String
    Private oRecordSet As SAPbobsCOM.Recordset

    Public Sub LoadForm()
        Try
            oForm = oApplication.Utilities.LoadForm(xml_Z_OutBound, frm_Z_OutBound)
            oForm = oApplication.SBO_Application.Forms.ActiveForm
            oForm.Freeze(True)
            oForm.PaneLevel = 1
            initialize(oForm)
            oForm.Freeze(False)
        Catch ex As Exception
            oApplication.Utilities.Message(ex.Message, SAPbouiCOM.BoStatusBarMessageType.smt_Error)
        End Try
    End Sub

    Private Sub initialize(ByRef oForm As SAPbouiCOM.Form)
        Try
            oForm.Items.Item("4").TextStyle = SAPbouiCOM.BoTextStyle.ts_BOLD
            oForm.Items.Item("8").TextStyle = 5
            oForm.Items.Item("_10").TextStyle = 5
            oForm.Items.Item("_19").TextStyle = 5
            oForm.DataSources.DataTables.Add("dtOrder")
            oForm.DataSources.DataTables.Add("dtSuccess")
            oForm.DataSources.DataTables.Add("dtFailure")
            loadCombo(oForm)
        Catch ex As Exception
            oApplication.Utilities.Message(ex.Message, SAPbouiCOM.BoStatusBarMessageType.smt_Error)
        End Try
    End Sub

#Region "Menu Event"
    Public Overrides Sub MenuEvent(ByRef pVal As SAPbouiCOM.MenuEvent, ByRef BubbleEvent As Boolean)
        Try
            Select Case pVal.MenuUID
                Case mnu_Z_OutBound
                    LoadForm()
                Case mnu_FIRST, mnu_LAST, mnu_NEXT, mnu_PREVIOUS
            End Select
        Catch ex As Exception
            oApplication.Utilities.Message(ex.Message, SAPbouiCOM.BoStatusBarMessageType.smt_Error)
            oForm.Freeze(False)
        End Try
    End Sub
#End Region

#Region "Item Event"
    Public Overrides Sub ItemEvent(ByVal FormUID As String, ByRef pVal As SAPbouiCOM.ItemEvent, ByRef BubbleEvent As Boolean)
        Try
            Dim oDataTable As SAPbouiCOM.DataTable
            If pVal.FormTypeEx = frm_Z_OutBound Then
                Select Case pVal.BeforeAction
                    Case True
                        Select Case pVal.EventType
                            Case SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED
                                oForm = oApplication.SBO_Application.Forms.Item(FormUID)
                                If (pVal.ItemUID = "7" Or pVal.ItemUID = "3") And oForm.PaneLevel > 1 Then
                                    If validation(oForm) Then
                                        If pVal.ItemUID = "7" Then
                                            If oApplication.SBO_Application.MessageBox("Do you want to Proceed?", , "Yes", "No") = 2 Then
                                                BubbleEvent = False
                                                Exit Sub
                                            Else
                                                If oForm.PaneLevel = 3 Then
                                                    If ExportGrid(oForm) Then
                                                        oSuccessGrid = oForm.Items.Item("9").Specific
                                                        oSuccessGrid.DataTable = oForm.DataSources.DataTables.Item("dtSuccess")
                                                        oSuccessGrid = oForm.Items.Item("10").Specific
                                                        oSuccessGrid.DataTable = oForm.DataSources.DataTables.Item("dtFailure")
                                                        oForm.PaneLevel = 4
                                                    End If
                                                End If
                                            End If
                                        End If
                                    Else
                                        BubbleEvent = False
                                        Exit Sub
                                    End If
                                End If
                        End Select
                    Case False
                        Select Case pVal.EventType
                            Case SAPbouiCOM.BoEventTypes.et_FORM_LOAD
                                oForm = oApplication.SBO_Application.Forms.Item(FormUID)
                            Case SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED
                                oForm = oApplication.SBO_Application.Forms.Item(FormUID)
                                If pVal.ItemUID = "3" And (oForm.PaneLevel = 1) Then
                                    oForm.PaneLevel = oForm.PaneLevel + 1
                                ElseIf pVal.ItemUID = "6" And (oForm.PaneLevel > 1) Then
                                    oForm.PaneLevel = oForm.PaneLevel - 1
                                ElseIf pVal.ItemUID = "3" And (oForm.PaneLevel = 2) Then
                                    LoadGrid(oForm)
                                    oForm.PaneLevel = oForm.PaneLevel + 1
                                ElseIf pVal.ItemUID = "11" And (oForm.PaneLevel = 4) Then
                                    oForm.Close()
                                    Exit Sub

                                ElseIf pVal.ItemUID = "17" Then
                                    selectAll(oForm)
                                ElseIf pVal.ItemUID = "18" Then
                                    clearAll(oForm)
                                End If
                            Case SAPbouiCOM.BoEventTypes.et_CHOOSE_FROM_LIST

                            Case SAPbouiCOM.BoEventTypes.et_LOST_FOCUS

                            Case SAPbouiCOM.BoEventTypes.et_FORM_RESIZE
                                'If oForm.State = SAPbouiCOM.BoFormStateEnum.fs_Maximized Or oForm.State = SAPbouiCOM.BoFormStateEnum.fs_Restore Then
                                '    oForm = oApplication.SBO_Application.Forms.Item(FormUID)
                                '    Try
                                '        reDrawForm(oForm)
                                '    Catch ex As Exception

                                '    End Try
                                'End If
                        End Select
                End Select
            End If
        Catch ex As Exception
            oForm.Freeze(False)
            oApplication.Utilities.Message(ex.Message, SAPbouiCOM.BoStatusBarMessageType.smt_Error)
            oForm = oApplication.SBO_Application.Forms.Item(FormUID)
        End Try
    End Sub
#End Region

    Private Sub LoadGrid(ByVal aform As SAPbouiCOM.Form)
        Try
            oForm.Freeze(True)

            Dim strFromDate, strToDate As String
            Dim strType As String = String.Empty
            strFromDate = oForm.Items.Item("15").Specific.value
            strToDate = oForm.Items.Item("16").Specific.value

            Try
                strType = CType(oForm.Items.Item("13").Specific, SAPbouiCOM.ComboBox).Selected.Value
            Catch ex As Exception
            End Try


            oOrderGrid = oForm.Items.Item("5").Specific
            oOrderGrid.DataTable = oForm.DataSources.DataTables.Item("dtOrder")

            oSuccessGrid = oForm.Items.Item("9").Specific
            oSuccessGrid.DataTable = oForm.DataSources.DataTables.Item("dtSuccess")

            oFailureGrid = oForm.Items.Item("10").Specific
            oFailureGrid.DataTable = oForm.DataSources.DataTables.Item("dtFailure")

            If strType = "GRPO" Then

                strqry = "Select  Distinct Convert(VarChar(1),'Y') As 'Select', T0.DocEntry,T0.DocNum, T0.CardCode, T0.CardName, CONVERT(VARCHAR(11),T1.DocDate,6) As 'DocDate' "
                strqry += " From OPDN T0 JOIN PDN1 T1 ON T0.DocEntry = T1.DocEntry Where 1 = 1 AND IsNull(T0.U_Export, 'N')='N'"
                If (strFromDate.Length > 0 And strToDate.Length > 0) Then
                    strqry += " And Convert(VarChar(8),T1.DocDate,112) Between '" + strFromDate + "' AND '" + strToDate + "'"
                End If
                If ((strFromDate.Length > 0) And (strToDate.Length = 0)) Then
                    strqry += " And Convert(VarChar(8),T1.DocDate,112) = '" + strFromDate + "'"
                End If
                If ((strFromDate.Length = 0) And (strToDate.Length > 0)) Then
                    strqry += " And Convert(VarChar(8),T1.DocDate,112) = '" + strToDate + "'"
                End If

            ElseIf strType = "GR" Then

                strqry = "Select  Distinct Convert(VarChar(1),'Y') As 'Select', T0.DocEntry,T0.DocNum, T0.CardCode, T0.CardName, CONVERT(VARCHAR(11),T1.DocDate,6) As 'DocDate' "
                strqry += " From ORPD T0 JOIN RPD1 T1 ON T0.DocEntry = T1.DocEntry Where 1 = 1 AND IsNull(T0.U_Export, 'N')='N'"
                If (strFromDate.Length > 0 And strToDate.Length > 0) Then
                    strqry += " And Convert(VarChar(8),T1.DocDate,112) Between '" + strFromDate + "' AND '" + strToDate + "'"
                End If
                If ((strFromDate.Length > 0) And (strToDate.Length = 0)) Then
                    strqry += " And Convert(VarChar(8),T1.DocDate,112) = '" + strFromDate + "'"
                End If
                If ((strFromDate.Length = 0) And (strToDate.Length > 0)) Then
                    strqry += " And Convert(VarChar(8),T1.DocDate,112) = '" + strToDate + "'"
                End If


            ElseIf strType = "APCreditMemo" Then

                strqry = "Select  Distinct Convert(VarChar(1),'Y') As 'Select', T0.DocEntry,T0.DocNum, T0.CardCode, T0.CardName, CONVERT(VARCHAR(11),T1.DocDate,6) As 'DocDate' "
                strqry += " From ORPC T0 JOIN RPC1 T1 ON T0.DocEntry = T1.DocEntry Where 1 = 1 AND IsNull(T0.U_Export, 'N')='N'"
                If (strFromDate.Length > 0 And strToDate.Length > 0) Then
                    strqry += " And Convert(VarChar(8),T1.DocDate,112) Between '" + strFromDate + "' AND '" + strToDate + "'"
                End If
                If ((strFromDate.Length > 0) And (strToDate.Length = 0)) Then
                    strqry += " And Convert(VarChar(8),T1.DocDate,112) = '" + strFromDate + "'"
                End If
                If ((strFromDate.Length = 0) And (strToDate.Length > 0)) Then
                    strqry += " And Convert(VarChar(8),T1.DocDate,112) = '" + strToDate + "'"
                End If


            ElseIf strType = "Inventory" Then

                strqry = "Select  Distinct Convert(VarChar(1),'Y') As 'Select', T1.DocEntry,T1.DocNum, T2.ItemName From OIQR T1 JOIN IQR1 T2 ON T1.DocEntry = T2.DocEntry Where IsNull(T1.U_Export, 'N')='N'"
                If (strFromDate.Length > 0 And strToDate.Length > 0) Then
                    strqry += " And Convert(VarChar(8),T1.DocDate,112) Between '" + strFromDate + "' AND '" + strToDate + "'"
                End If
                If ((strFromDate.Length > 0) And (strToDate.Length = 0)) Then
                    strqry += " And Convert(VarChar(8),T1.DocDate,112) = '" + strFromDate + "'"
                End If
                If ((strFromDate.Length = 0) And (strToDate.Length > 0)) Then
                    strqry += " And Convert(VarChar(8),T1.DocDate,112) = '" + strToDate + "'"
                End If

            End If

            oOrderGrid.DataTable.ExecuteQuery(strqry)

            gridFormat(oForm, strType)
            fillHeader(oForm, "5")
            oForm.Freeze(False)
        Catch ex As Exception
            oForm.Freeze(False)
        End Try

    End Sub

    Private Sub gridFormat(ByVal oform As SAPbouiCOM.Form, ByVal strType As String)
        Try
            If strType = "GRPO" Or strType = "GR" Or strType = "APCreditMemo" Then

                oOrderGrid = oform.Items.Item("5").Specific
                oOrderGrid.DataTable = oform.DataSources.DataTables.Item("dtOrder")

                oOrderGrid.Columns.Item("Select").TitleObject.Caption = "Select"
                oOrderGrid.Columns.Item("Select").Type = SAPbouiCOM.BoGridColumnType.gct_CheckBox
                oOrderGrid.Columns.Item("Select").Editable = True

                oOrderGrid.Columns.Item("DocEntry").TitleObject.Caption = "Doc Entry"
                oOrderGrid.Columns.Item("DocEntry").Type = SAPbouiCOM.BoGridColumnType.gct_EditText
                oEditTextColumn = oOrderGrid.Columns.Item("DocEntry")
                oOrderGrid.Columns.Item("DocEntry").Editable = False

                oOrderGrid.Columns.Item("DocNum").TitleObject.Caption = "Order No."
                oOrderGrid.Columns.Item("DocNum").Editable = False

                oOrderGrid.Columns.Item("CardCode").TitleObject.Caption = "Card Code"
                oOrderGrid.Columns.Item("CardCode").Visible = False
                oOrderGrid.Columns.Item("DocNum").Editable = False

                oOrderGrid.Columns.Item("CardName").TitleObject.Caption = "Card Name"
                oOrderGrid.Columns.Item("CardName").Editable = False

                oOrderGrid.Columns.Item("DocDate").TitleObject.Caption = "Doc Date"
                oOrderGrid.Columns.Item("DocDate").Editable = False

            ElseIf strType = "Inventory" Then

                oOrderGrid = oform.Items.Item("5").Specific
                oOrderGrid.DataTable = oform.DataSources.DataTables.Item("dtOrder")

                oOrderGrid.Columns.Item("Select").TitleObject.Caption = "Select"
                oOrderGrid.Columns.Item("Select").Type = SAPbouiCOM.BoGridColumnType.gct_CheckBox
                oOrderGrid.Columns.Item("Select").Editable = True

                oOrderGrid.Columns.Item("DocEntry").TitleObject.Caption = "Doc Entry"
                oOrderGrid.Columns.Item("DocEntry").Type = SAPbouiCOM.BoGridColumnType.gct_EditText
                oEditTextColumn = oOrderGrid.Columns.Item("DocEntry")
                oOrderGrid.Columns.Item("DocEntry").Editable = False

                oOrderGrid.Columns.Item("DocNum").TitleObject.Caption = "Order No."
                oOrderGrid.Columns.Item("DocNum").Editable = False

                oOrderGrid.Columns.Item("ItemName").TitleObject.Caption = "Item Name"
                oOrderGrid.Columns.Item("ItemName").Editable = False

                oOrderGrid.Columns.Item("DocDate").TitleObject.Caption = "Doc Date"
                oOrderGrid.Columns.Item("DocDate").Editable = False

            End If
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Private Sub fillHeader(ByVal aForm As SAPbouiCOM.Form, ByVal strGridID As String)
        Try
            Dim oGrid As SAPbouiCOM.Grid
            aForm.Freeze(True)
            oGrid = aForm.Items.Item(strGridID).Specific
            oGrid.RowHeaders.TitleObject.Caption = "#"
            For index As Integer = 0 To oGrid.DataTable.Rows.Count - 1
                oGrid.RowHeaders.SetText(index, (index + 1).ToString())
            Next
            aForm.Freeze(False)
        Catch ex As Exception
            aForm.Freeze(False)
            Throw ex
        End Try
    End Sub

    Private Function validation(ByVal aForm As SAPbouiCOM.Form) As Boolean
        Dim _retVal As Boolean = True
        Try
            If oForm.PaneLevel = 2 Then

                Dim strStype As String = oForm.Items.Item("13").Specific.value
                If strStype.Trim() = "" Then
                    oApplication.Utilities.Message("Select Type...", SAPbouiCOM.BoStatusBarMessageType.smt_Error)
                    Return _retVal = False
                End If
            End If


            If oForm.PaneLevel = 3 Then
                oOrderGrid = oForm.Items.Item("5").Specific
                For intRow As Integer = 0 To oOrderGrid.Rows.Count - 1
                    If oOrderGrid.DataTable.GetValue("Select", intRow).ToString() = "Y" Then
                        _retVal = True
                        Exit For
                    End If
                Next

                If _retVal = False Then
                    oApplication.Utilities.Message("No Records Selected", SAPbouiCOM.BoStatusBarMessageType.smt_Error)
                End If
            End If

            Return _retVal
        Catch ex As Exception
            Throw ex
        End Try
    End Function

    Private Function ExportGrid(ByVal oForm As SAPbouiCOM.Form) As Boolean
        Try

            Dim _retVal As Boolean = False
            Dim intDocEntry As Integer
            Dim strDocNum As String = String.Empty
            Dim strCardName As String = String.Empty
            Dim strDocDate As String = String.Empty
            Dim strDFNP As String

            Dim strType As String = String.Empty


            oDTSuccess = oForm.DataSources.DataTables.Item("dtSuccess")
            oDTFailure = oForm.DataSources.DataTables.Item("dtFailure")

            strType = CType(oForm.Items.Item("13").Specific, SAPbouiCOM.ComboBox).Selected.Value

            Try
                oDTSuccess.Columns.Add("DocEntry", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric, 10)
                oDTSuccess.Columns.Add("DocNum", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric, 10)
                If strType = "Inventory" Then
                    oDTSuccess.Columns.Add("ItemName", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric, 100)
                Else
                    oDTSuccess.Columns.Add("CardName", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric, 100)
                End If
                oDTSuccess.Columns.Add("DocDate", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric, 25)

                oDTFailure.Columns.Add("DocEntry", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric, 10)
                oDTFailure.Columns.Add("DocNum", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric, 10)
                If strType = "Inventory" Then
                    oDTFailure.Columns.Add("ItemName", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric, 100)
                Else
                    oDTFailure.Columns.Add("CardName", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric, 100)
                End If
                oDTFailure.Columns.Add("DocDate", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric, 25)
                oDTFailure.Columns.Add("FailedReason", SAPbouiCOM.BoFieldsType.ft_AlphaNumeric, 250)
            Catch ex As Exception

            End Try
           

            If strType = "GRPO" Then
                DSTOXML(oForm, strType)
            ElseIf strType = "GR" Then
                DSTOXML(oForm, strType)
            ElseIf strType = "APCreditMemo" Then
                DSTOXML(oForm, strType)
            ElseIf strType = "Inventory" Then
                DSTOXML(oForm, strType)
            End If

            _retVal = True
            Return _retVal
        Catch ex As Exception
            Throw ex
        End Try
    End Function

    Private Sub addEmptyElementsToXML(ByVal dataSet As DataSet)
        For Each dataTable As DataTable In dataSet.Tables
            For Each dataRow As DataRow In dataTable.Rows
                For j As Integer = 0 To dataRow.ItemArray.Length - 1
                    If IsDBNull(dataRow.ItemArray(j)) Then
                        dataRow(j) = String.Empty
                    End If
                Next
            Next
        Next
    End Sub

    Private Sub loadCombo(ByVal oForm As SAPbouiCOM.Form)
        Try
            oCombo = oForm.Items.Item("13").Specific
            With oCombo
                .ValidValues.Add("GRPO", "GRPO")
                .ValidValues.Add("GR", "Goods Return")
                .ValidValues.Add("APCreditMemo", "AP Credit Memo")
                .ValidValues.Add("Inventory", "Inventory Stock Issue")
            End With
        Catch ex As Exception
            oApplication.Utilities.Message(ex.Message, SAPbouiCOM.BoStatusBarMessageType.smt_Error)
        End Try
    End Sub

    Private Sub selectAll(ByVal oForm As SAPbouiCOM.Form)
        Try
            oOrderGrid = oForm.Items.Item("5").Specific
            oForm.Freeze(True)
            For index As Integer = 0 To oOrderGrid.DataTable.Rows.Count - 1
                oOrderGrid.DataTable.SetValue("Select", index, "Y")
            Next
            oForm.Freeze(False)
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Private Sub clearAll(ByVal oForm As SAPbouiCOM.Form)
        Try
            oOrderGrid = oForm.Items.Item("5").Specific
            oForm.Freeze(True)
            For index As Integer = 0 To oOrderGrid.DataTable.Rows.Count - 1
                oOrderGrid.DataTable.SetValue("Select", index, "N")
            Next
            oForm.Freeze(False)
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Public Function GetFilePath(ByVal Type As String) As String
        Dim _retVal As String
        Try
            oRecordSet = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
            oRecordSet.DoQuery("Select T1.U_ExpPath From [@Z_HDF_OBND] T1 Where T1.U_Type='" & Type & "'")
            If Not oRecordSet.EoF Then
                _retVal = oRecordSet.Fields.Item(0).Value
            Else
                Throw New Exception("")
            End If
        Catch generatedExceptionName As Exception
            Throw
        End Try
        Return _retVal
    End Function

    Public Function GetFrenchName(ByVal EnglishName As String) As DataTable
        Dim myConnection As New SqlConnection
        Dim oCommand As SqlCommand
        Dim oSqlAdap As SqlDataAdapter

        Dim args() As Object = {oApplication.Company.Server, oApplication.Company.CompanyDB, oApplication.Company.DbUserName}
        Dim strConnection As String = String.Format(ConfigurationManager.AppSettings("ConnectionString").ToString(), args)
        myConnection = New SqlConnection(strConnection)
        Dim _retVal As New DataTable

        Try
            myConnection.Open()
            oCommand = New SqlCommand()
            If myConnection.State = ConnectionState.Open Then
                oCommand.Connection = myConnection
                Dim strquery As String = "Select T1.U_FName From [@Z_HDF_OBD1] T1 Where T1.U_EName='" & EnglishName & "'"
                oCommand.CommandText = strquery
                oCommand.CommandType = CommandType.Text
                oSqlAdap = New SqlDataAdapter(oCommand)
                oSqlAdap.Fill(_retVal)
            Else
                Throw New Exception("")
            End If
        Catch generatedExceptionName As Exception
            Throw
        Finally
            myConnection.Close()
            oCommand = Nothing
            oSqlAdap = Nothing
        End Try
        Return _retVal
    End Function

    Private Sub DSTOXML(ByVal oForm As SAPbouiCOM.Form, ByVal strType As String)
        Dim intDocEntry As Integer
        Dim strDocNum As String = String.Empty
        Dim strCardName As String = String.Empty
        Dim strDocDate As String = String.Empty
        Dim intID_S As Integer = 0
        Dim intID_F As Integer = 0
        Dim intStatus As Integer

        Try
            For intRow As Integer = 0 To oOrderGrid.Rows.Count - 1
                If oOrderGrid.DataTable.GetValue("Select", intRow).ToString() = "Y" Then

                    intDocEntry = CInt(oOrderGrid.DataTable.GetValue("DocEntry", intRow).ToString())
                    strDocNum = oOrderGrid.DataTable.GetValue("DocNum", intRow).ToString()
                    If strType = "Inventory" Then
                        strCardName = oOrderGrid.DataTable.GetValue("ItemName", intRow).ToString()
                    Else
                        strCardName = oOrderGrid.DataTable.GetValue("CardName", intRow).ToString()
                    End If

                    Dim dataSet As New DataSet

                    Dim EXPath As String = GetFilePath(strType)

                    Dim strFileExt As String = ".xml"
                    Dim strDFileName As String = intDocEntry.ToString + strFileExt
                    Dim strDPath As String = EXPath & "\" + strDFileName
                    Dim strDFNP As String = strDPath
                    Dim strQuery As String

                    Dim objCommand As SqlCommand
                    Dim objConnection As SqlConnection
                    Dim objAdapter As SqlDataAdapter

                    Dim args() As Object = {oApplication.Company.Server, oApplication.Company.CompanyDB, oApplication.Company.DbUserName}
                    Dim strConnection As String = String.Format(ConfigurationManager.AppSettings("ConnectionString").ToString(), args)
                 
                    objConnection = New SqlConnection(strConnection)
                    objConnection.Open()
                    strQuery = "Exec " & strType & " '" & intDocEntry & "'"
                    objCommand = New SqlCommand(strQuery, objConnection)
                    objCommand.CommandTimeout = 300
                    objAdapter = New SqlDataAdapter(objCommand)
                    dataSet = New DataSet()
                    objAdapter.Fill(dataSet)
                    objConnection.Close()

                    If dataSet IsNot Nothing AndAlso dataSet.Tables.Count > 0 Then
                        dataSet.Tables(0).TableName = "Documents"
                        dataSet.Tables(1).TableName = "Articles"
                        dataSet.Tables(2).TableName = "Article"
                        dataSet.Tables(3).TableName = "Lots"
                        dataSet.Tables(4).TableName = "Lot"
                    End If

                    Dim Document As DataRelation = dataSet.Relations.Add("DocumentDetail", dataSet.Tables("Documents").Columns("DocEntry"), dataSet.Tables("Articles").Columns("DocEntry"))
                    Dim Details As DataRelation = dataSet.Relations.Add("DDetails", dataSet.Tables("Articles").Columns("DocEntry"), dataSet.Tables("Article").Columns("DocEntry"))
                    Dim Detail As DataRelation
                    Dim LotsLot As DataRelation

                    If strType = "Inventory" Then
                        Detail = dataSet.Relations.Add("DetailsLot", dataSet.Tables("Article").Columns("Key"), dataSet.Tables("Lots").Columns("Key"))
                        LotsLot = dataSet.Relations.Add("LotsLot", dataSet.Tables("Lots").Columns("Key"), dataSet.Tables("Lot").Columns("Key"))
                    Else
                        Detail = dataSet.Relations.Add("DetailsLot", dataSet.Tables("Article").Columns("LineNum"), dataSet.Tables("Lots").Columns("LineNum"))
                        LotsLot = dataSet.Relations.Add("LotsLot", dataSet.Tables("Lots").Columns("Key"), dataSet.Tables("Lot").Columns("Key"))
                    End If

                    Document.Nested = True
                    Details.Nested = True
                    Detail.Nested = True
                    LotsLot.Nested = True

                    dataSet.Tables("Documents").Columns("DocEntry").ColumnMapping = MappingType.Hidden
                    dataSet.Tables("Articles").Columns("DocEntry").ColumnMapping = MappingType.Hidden
                    dataSet.Tables("Article").Columns("DocEntry").ColumnMapping = MappingType.Hidden
                    If strType = "Inventory" Then
                        dataSet.Tables("Article").Columns("Key").ColumnMapping = MappingType.Hidden
                        dataSet.Tables("Lots").Columns("Key").ColumnMapping = MappingType.Hidden
                    Else
                        dataSet.Tables("Article").Columns("LineNum").ColumnMapping = MappingType.Hidden
                        dataSet.Tables("Lots").Columns("DocEntry").ColumnMapping = MappingType.Hidden
                        dataSet.Tables("Lots").Columns("LineNum").ColumnMapping = MappingType.Hidden
                    End If
                    dataSet.Tables("Lots").Columns("Key").ColumnMapping = MappingType.Hidden
                    dataSet.Tables("Lot").Columns("Key").ColumnMapping = MappingType.Hidden


                    For i As Integer = 0 To dataSet.Tables("Documents").Columns.Count - 1
                        Dim Dt As DataTable = GetFrenchName(dataSet.Tables("Documents").Columns(i).Caption)
                        If Not IsNothing(Dt) And Dt.Rows.Count > 0 Then
                            dataSet.Tables("Documents").Columns(i).ColumnName = Dt.Rows(0).Item(0).ToString
                        End If
                    Next
                    dataSet.AcceptChanges()

                    For i As Integer = 0 To dataSet.Tables("Article").Columns.Count - 1
                        Dim Dt As DataTable = GetFrenchName(dataSet.Tables("Article").Columns(i).Caption)
                        If Not IsNothing(Dt) And Dt.Rows.Count > 0 Then
                            dataSet.Tables("Article").Columns(i).ColumnName = Dt.Rows(0).Item(0).ToString
                        End If
                    Next
                    dataSet.AcceptChanges()

                    For i As Integer = 0 To dataSet.Tables("Lot").Columns.Count - 1
                        Dim Dt As DataTable = GetFrenchName(dataSet.Tables("Lot").Columns(i).Caption)
                        If Not IsNothing(Dt) And Dt.Rows.Count > 0 Then
                            dataSet.Tables("Lot").Columns(i).ColumnName = Dt.Rows(0).Item(0).ToString
                        End If
                    Next

                    dataSet.AcceptChanges()

                    addEmptyElementsToXML(dataSet)

                    Dim FND As DataTable = GetFrenchName("Documents")
                    If Not IsNothing(FND) And FND.Rows.Count > 0 Then
                        dataSet.Tables("Documents").TableName = FND.Rows(0).Item(0).ToString
                    End If
                    dataSet.AcceptChanges()
                    dataSet.WriteXml(strDFNP, XmlWriteMode.WriteSchema)


                    Dim w As XmlWriter = New XmlTextWriter(strDFNP, Encoding.UTF8)
                    w.WriteProcessingInstruction("xml", "version='1.0' encoding='UTF-8'")
                    Dim xd As New XmlDataDocument(dataSet)
                    Dim xdNew As New XmlDataDocument()
                    dataSet.EnforceConstraints = False
                    Dim node As XmlNode = xdNew.ImportNode(xd.DocumentElement.LastChild, True)
                    node.WriteTo(w)
                    w.Close()

                    If File.Exists(strDFNP) Then
                        If strType = "GRPO" Then
                            strQuery = "Update OPDN Set U_Export='Y' Where DocEntry=" & intDocEntry
                        ElseIf strType = "GR" Then
                            strQuery = "Update ORPD Set U_Export='Y' Where DocEntry=" & intDocEntry
                        ElseIf strType = "APCreditMemo" Then
                            strQuery = "Update ORPC Set U_Export='Y' Where DocEntry=" & intDocEntry
                        ElseIf strType = "Inventory" Then
                            strQuery = "Update OIQR Set U_Export='Y' Where DocEntry=" & intDocEntry
                        End If
                        oRecordSet.DoQuery(strQuery)


                        'Updating Log
                        Dim oUserTable As SAPbobsCOM.UserTable
                        oUserTable = oApplication.Company.UserTables.Item("Z_HDF_OBND_LOG")

                        strQuery = "Select count(*) As Code From [@Z_HDF_OBND_Log]"
                        oRecordSet.DoQuery(strQuery)
                        'Set default, mandatory fields
                        If oRecordSet.RecordCount > 0 Then
                            oUserTable.Code = (CInt(oRecordSet.Fields.Item("Code").Value) + 1).ToString()
                            oUserTable.Name = (CInt(oRecordSet.Fields.Item("Code").Value) + 1).ToString()
                        Else
                            oUserTable.Code = "1"
                            oUserTable.Name = "1"
                        End If
                        'Set user field
                        oUserTable.UserFields.Fields.Item("U_Type").Value = strType
                        oUserTable.UserFields.Fields.Item("U_DocNum").Value = intDocEntry.ToString()
                        oUserTable.UserFields.Fields.Item("U_Status").Value = "Y"
                        Dim now As DateTime = DateTime.Now
                        oUserTable.UserFields.Fields.Item("U_ProDate").Value = now.ToString("d")
                        oUserTable.UserFields.Fields.Item("U_ProTime").Value = now.ToString("HH:MM")

                        oUserTable.UserFields.Fields.Item("U_Remarks").Value = "Exported Successfully"

                        oUserTable.Add()

                        oDTSuccess.Rows.Add(1)
                        oDTSuccess.SetValue("DocEntry", intID_S, intDocEntry)
                        oDTSuccess.SetValue("DocNum", intID_S, strDocNum)
                        If strType = "Inventory" Then
                            oDTSuccess.SetValue("ItemName", intID_S, strCardName)
                        Else
                            oDTSuccess.SetValue("CardName", intID_S, strCardName)
                        End If

                        oDTSuccess.SetValue("DocDate", intID_S, strDocDate)
                        intID_S += 1


                    Else
                        'Updating Log
                        Dim oUserTable As SAPbobsCOM.UserTable
                        oUserTable = oApplication.Company.UserTables.Item("Z_HDF_OBND_LOG")

                        strQuery = "Select count(*) As Code From [@Z_HDF_OBND_Log]"
                        oRecordSet.DoQuery(strQuery)
                        'Set default, mandatory fields
                        If oRecordSet.RecordCount > 0 Then
                            oUserTable.Code = (CInt(oRecordSet.Fields.Item("Code").Value) + 1).ToString()
                            oUserTable.Name = (CInt(oRecordSet.Fields.Item("Code").Value) + 1).ToString()
                        Else
                            oUserTable.Code = "1"
                            oUserTable.Name = "1"
                        End If
                        'Set user field
                        oUserTable.UserFields.Fields.Item("U_Type").Value = strType
                        oUserTable.UserFields.Fields.Item("U_DocNum").Value = strDocNum
                        oUserTable.UserFields.Fields.Item("U_Status").Value = "N"
                        Dim now As DateTime = DateTime.Now
                        oUserTable.UserFields.Fields.Item("U_ProDate").Value = now.ToString("d")
                        oUserTable.UserFields.Fields.Item("U_ProTime").Value = now.ToString("HH:MM")
                        oUserTable.UserFields.Fields.Item("U_Remarks").Value = oApplication.Company.GetLastErrorDescription().ToString()


                        oDTFailure.Rows.Add(1)
                        oDTFailure.SetValue("DocEntry", intID_S, intDocEntry)
                        oDTFailure.SetValue("DocNum", intID_S, strDocNum)
                        If strType = "Inventory" Then
                            oDTFailure.SetValue("ItemName", intID_S, strCardName)
                        Else
                            oDTFailure.SetValue("CardName", intID_S, strCardName)
                        End If
                        oDTFailure.SetValue("DocDate", intID_S, strDocDate)
                        oDTFailure.SetValue("FailReason", intID_F, oApplication.Company.GetLastErrorDescription().ToString())
                        intID_F += 1

                    End If

                End If
            Next

        Catch ex As Exception
            Throw ex
        End Try
    End Sub

End Class

