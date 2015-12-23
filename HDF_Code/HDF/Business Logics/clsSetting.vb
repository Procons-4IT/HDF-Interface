Imports SAPbobsCOM
Imports System.Windows.Forms

Public Class clsSetting
    Inherits clsBase


    Private oMatrix As SAPbouiCOM.Matrix
    Private objForm As SAPbouiCOM.Form
    Dim oDBDataSource As SAPbouiCOM.DBDataSource
    Dim oDBDataSourceLines As SAPbouiCOM.DBDataSource
    Private oEditText As SAPbouiCOM.EditText
    Private oMode As SAPbouiCOM.BoFormMode
    Private oCombo As SAPbouiCOM.ComboBox
    Private oCombo1 As SAPbouiCOM.ComboBox
    Private oRecordSet As SAPbobsCOM.Recordset
    Private strQuery As String
    Public intSelectedMatrixrow As Integer = 0
    Private RowtoDelete As Integer
    Dim count As Integer

    Public Sub New()
        MyBase.New()
    End Sub

    Private Sub LoadForm()
        Try
            If oApplication.Utilities.validateAuthorization(oApplication.Company.UserSignature, frm_Z_Setting) = False Then
                oApplication.Utilities.Message("You are not authorized to do this action", SAPbouiCOM.BoStatusBarMessageType.smt_Error)
                Exit Sub
            End If

            oForm = oApplication.Utilities.LoadForm(xml_Z_Setting, frm_Z_Setting)
            oForm = oApplication.SBO_Application.Forms.ActiveForm()
            initialize(oForm)
            loadCombo(oForm)
            oForm.EnableMenu(mnu_ADD_ROW, True)
            oForm.EnableMenu(mnu_DELETE_ROW, True)
            oForm.Items.Item("4").Click(SAPbouiCOM.BoCellClickType.ct_Regular)

        Catch ex As Exception
            oForm.Freeze(False)
        End Try
    End Sub

#Region "Menu Event"
    Public Overrides Sub MenuEvent(ByRef pVal As SAPbouiCOM.MenuEvent, ByRef BubbleEvent As Boolean)
        Try
            Select Case pVal.BeforeAction
                Case True
                    Select Case pVal.MenuUID

                    End Select
                Case False
                    Select Case pVal.MenuUID
                        Case mnu_Z_Setting
                            LoadForm()
                        Case mnu_ADD
                            oForm = oApplication.SBO_Application.Forms.ActiveForm()
                            'oForm.Items.Item("9").Enabled = False
                            initialize(oForm)
                            oForm.Items.Item("4").Click(SAPbouiCOM.BoCellClickType.ct_Regular)
                        Case mnu_FIND
                            oForm = oApplication.SBO_Application.Forms.ActiveForm()
                            oForm.Items.Item("4").Enabled = True
                        Case mnu_ADD_ROW
                            If pVal.BeforeAction = False Then
                                AddRow(oForm, "8")
                                If oForm.Mode = SAPbouiCOM.BoFormMode.fm_OK_MODE Then oForm.Mode = SAPbouiCOM.BoFormMode.fm_UPDATE_MODE
                            End If
                        Case mnu_DELETE_ROW
                            RefereshDeleteRow(oForm, "8")
                    End Select
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
            If pVal.FormTypeEx = frm_Z_Setting Then
                Select Case pVal.BeforeAction
                    Case True
                        Select Case pVal.EventType
                            Case SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED
                                oForm = oApplication.SBO_Application.Forms.Item(FormUID)
                                If pVal.ItemUID = "1" And _
                                    (oForm.Mode = SAPbouiCOM.BoFormMode.fm_ADD_MODE Or oForm.Mode = SAPbouiCOM.BoFormMode.fm_UPDATE_MODE) Then
                                    If oForm.Mode = SAPbouiCOM.BoFormMode.fm_ADD_MODE Or oForm.Mode = SAPbouiCOM.BoFormMode.fm_UPDATE_MODE Then
                                        If validation(oForm) Then
                                            If oApplication.SBO_Application.MessageBox("Do you want to confirm the information?", , "Yes", "No") = 2 Then
                                                BubbleEvent = False
                                                Exit Sub
                                            End If
                                        Else
                                            BubbleEvent = False
                                            Exit Sub
                                        End If
                                    End If
                                End If
                        End Select
                    Case False
                        Select Case pVal.EventType
                            Case SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED
                                oForm = oApplication.SBO_Application.Forms.Item(FormUID)
                                Select Case pVal.ItemUID
                                    Case "1"
                                        If pVal.Action_Success Then
                                            If oForm.Mode = SAPbouiCOM.BoFormMode.fm_ADD_MODE Then
                                                initialize(oForm)
                                                oForm.Items.Item("4").Click(SAPbouiCOM.BoCellClickType.ct_Regular)
                                            End If
                                        End If
                                    Case "7"

                                        oApplication.Utilities.OpenFileDialogBox(oForm, "6")

                                End Select
                            Case SAPbouiCOM.BoEventTypes.et_LOST_FOCUS
                                oForm = oApplication.SBO_Application.Forms.Item(FormUID)
                                If Not oForm.Mode = SAPbouiCOM.BoFormMode.fm_FIND_MODE Then
                                    If (pVal.ItemUID = "8" _
                                                And (pVal.ColUID = "V_0") And pVal.Row > 0) Then

                                        alldataSource(oForm)
                                        oMatrix = oForm.Items.Item(pVal.ItemUID).Specific
                                        oMatrix.FlushToDataSource()
                                        oMatrix.LoadFromDataSource()
                                        oMatrix.FlushToDataSource()
                                        Select Case pVal.ItemUID
                                            Case "8"
                                                Dim strEName As String = oDBDataSourceLines.GetValue("U_EName", pVal.Row - 1)
                                                Dim strFName As String = oDBDataSourceLines.GetValue("U_FName", pVal.Row - 1)
                                                If strEName.Trim().Length = 0 And strFName.Trim().Length > 0 Then
                                                    oDBDataSourceLines.SetValue("U_FName", pVal.Row - 1, "")
                                                    oMatrix.LoadFromDataSource()
                                                    oMatrix.FlushToDataSource()
                                                    If oForm.Mode = SAPbouiCOM.BoFormMode.fm_OK_MODE Then oForm.Mode = SAPbouiCOM.BoFormMode.fm_UPDATE_MODE
                                                End If

                                        End Select
                                    End If
                                End If

                            Case SAPbouiCOM.BoEventTypes.et_CLICK
                                oForm = oApplication.SBO_Application.Forms.Item(FormUID)
                                alldataSource(oForm)
                                If (pVal.ItemUID = "8") Then
                                    intSelectedMatrixrow = pVal.Row
                                End If

                            Case SAPbouiCOM.BoEventTypes.et_CHOOSE_FROM_LIST

                            Case SAPbouiCOM.BoEventTypes.et_COMBO_SELECT

                        End Select
                End Select
            End If
        Catch ex As Exception
            oApplication.Utilities.Message(ex.Message, SAPbouiCOM.BoStatusBarMessageType.smt_Error)
            oForm = oApplication.SBO_Application.Forms.Item(FormUID)
            oForm.Freeze(False)
        End Try
    End Sub
#End Region

#Region "Data Event"

    Public Sub FormDataEvent(ByRef BusinessObjectInfo As SAPbouiCOM.BusinessObjectInfo, ByRef BubbleEvent As Boolean)
        Try
            Select Case BusinessObjectInfo.BeforeAction
                Case True
                Case False
                    Select Case BusinessObjectInfo.EventType
                        Case SAPbouiCOM.BoEventTypes.et_FORM_DATA_ADD
                            oForm = oApplication.SBO_Application.Forms.Item(BusinessObjectInfo.FormUID)
                            If BusinessObjectInfo.ActionSuccess Then

                            End If
                        Case SAPbouiCOM.BoEventTypes.et_FORM_DATA_UPDATE
                            oForm = oApplication.SBO_Application.Forms.Item(BusinessObjectInfo.FormUID)
                            If BusinessObjectInfo.ActionSuccess Then

                            End If
                        Case SAPbouiCOM.BoEventTypes.et_FORM_DATA_LOAD
                            If BusinessObjectInfo.ActionSuccess Then
                                'oForm.Items.Item("9").Enabled = False

                            End If
                    End Select
            End Select
        Catch ex As Exception
            oApplication.Utilities.Message(ex.Message, SAPbouiCOM.BoStatusBarMessageType.smt_Error)
            oForm.Freeze(False)
        End Try
    End Sub

#End Region
    Private Sub initialize(ByVal oForm As SAPbouiCOM.Form)
        Try

            oDBDataSource = oForm.DataSources.DBDataSources.Item("@Z_HDF_OBND")
            oDBDataSourceLines = oForm.DataSources.DBDataSources.Item("@Z_HDF_OBD1")


            oMatrix = oForm.Items.Item("8").Specific
            oMatrix.LoadFromDataSource()
            oMatrix.AddRow(1, -1)
            oMatrix.FlushToDataSource()

            oRecordSet = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
            oRecordSet.DoQuery("Select IsNull(MAX(DocEntry),0) +1 From [@Z_HDF_OBND]")
            If Not oRecordSet.EoF Then
                Dim intDocEntry As Integer = oRecordSet.Fields.Item(0).Value
                Dim strCode As String = String.Format(intDocEntry, "0000")
                oApplication.Utilities.setEditText(oForm, "9", strCode)
            End If

            oForm.Update()
        Catch ex As Exception
            Throw ex
        End Try

    End Sub

    Private Sub loadCombo(ByVal oForm As SAPbouiCOM.Form)
        Try
            oCombo = oForm.Items.Item("4").Specific

            With oCombo
                .ValidValues.Add("GRPO", "GRPO")
                .ValidValues.Add("GR", "Goods Return")
                .ValidValues.Add("APCreditMemo", "AP Credit Memo")
                .ValidValues.Add("Inventory", "Inventory Stock Issue")
            End With
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Private Sub addblankRow(ByVal oForm As SAPbouiCOM.Form, ByVal strItem As String)
        Try
            oMatrix = oForm.Items.Item(strItem).Specific
            oMatrix.LoadFromDataSource()
            oMatrix.AddRow(1, oMatrix.RowCount)
            oMatrix.FlushToDataSource()
            oMatrix.LoadFromDataSource()
            oMatrix.SelectionMode = SAPbouiCOM.BoMatrixSelect.ms_Single
            oMatrix.ClearRowData(oMatrix.RowCount)
            AssignLineNo(oForm, strItem)
            oMatrix.FlushToDataSource()
        Catch ex As Exception
            Throw ex
        End Try
    End Sub

    Private Sub AssignLineNo(ByVal aForm As SAPbouiCOM.Form, ByVal strItem As String)
        Try
            aForm.Freeze(True)
            oMatrix = aForm.Items.Item(strItem).Specific
            alldataSource(oForm)
            oMatrix.FlushToDataSource()

            Select Case strItem
                Case "8"
                    For count = 1 To oDBDataSourceLines.Size
                        oDBDataSourceLines.SetValue("LineId", count - 1, count)
                    Next
            End Select

            oMatrix.LoadFromDataSource()
            aForm.Freeze(False)
        Catch ex As Exception
            aForm.Freeze(False)
            Throw ex
        End Try
    End Sub

    Private Sub alldataSource(ByVal aForm As SAPbouiCOM.Form)
        Try
            oDBDataSource = oForm.DataSources.DBDataSources.Item("@Z_HDF_OBND")
            oDBDataSourceLines = oForm.DataSources.DBDataSources.Item("@Z_HDF_OBD1")


        Catch ex As Exception
            Throw ex
        End Try
    End Sub

   
    Private Sub AddRow(ByVal aForm As SAPbouiCOM.Form, ByVal strItem As String)
        Try
            aForm.Freeze(True)
            oMatrix = aForm.Items.Item(strItem).Specific
            oMatrix.FlushToDataSource()
            alldataSource(oForm)
            If oMatrix.RowCount <= 0 Then
                oMatrix.AddRow()
            Else
                If oApplication.Utilities.getMatrixValues(oMatrix, "V_1", oMatrix.RowCount) <> "" Then
                    oMatrix.AddRow(1, oMatrix.RowCount + 1)
                    oMatrix.ClearRowData(oMatrix.RowCount)
                    If aForm.Mode = SAPbouiCOM.BoFormMode.fm_OK_MODE Then
                        aForm.Mode = SAPbouiCOM.BoFormMode.fm_UPDATE_MODE
                    End If
                End If
            End If
            oMatrix.FlushToDataSource()
            For count = 1 To oDBDataSourceLines.Size
                oDBDataSourceLines.SetValue("LineId", count - 1, count)
            Next
            oMatrix.LoadFromDataSource()
            oMatrix.FlushToDataSource()
            AssignLineNo(aForm, strItem)
            oMatrix.Columns.Item("V_1").Cells.Item(oMatrix.RowCount).Click(SAPbouiCOM.BoCellClickType.ct_Regular)

            aForm.Freeze(False)
        Catch ex As Exception
            aForm.Freeze(False)
            Throw ex
        End Try
    End Sub

    Private Sub RefereshDeleteRow(ByVal aForm As SAPbouiCOM.Form, ByVal strItem As String)
        Try
            oMatrix = aForm.Items.Item(strItem).Specific
            alldataSource(oForm)

            Me.RowtoDelete = intSelectedMatrixrow
            oDBDataSourceLines.RemoveRecord(Me.RowtoDelete - 1)
            oMatrix.LoadFromDataSource()
            oMatrix.FlushToDataSource()
            For count = 0 To oDBDataSourceLines.Size - 1
                oDBDataSourceLines.SetValue("LineId", count, count + 1)
            Next
            oMatrix.LoadFromDataSource()
            oMatrix.FlushToDataSource()
        Catch ex As Exception
            aForm.Freeze(False)
            Throw ex
        End Try
    End Sub


    Private Function validation(ByVal aForm As SAPbouiCOM.Form) As Boolean
        Try
            Dim strType As String
            Dim strExportPath As String
           
            alldataSource(oForm)

            strType = oForm.Items.Item("4").Specific.value
            strExportPath = oForm.Items.Item("6").Specific.value

            If strType = "" Then
                oApplication.Utilities.Message("Select Type ...", SAPbouiCOM.BoStatusBarMessageType.smt_Error)
                Return False
            ElseIf strExportPath = "" Then
                oApplication.Utilities.Message("Select Export Path ...", SAPbouiCOM.BoStatusBarMessageType.smt_Error)
                Return False
            End If

            'oMatrix = oForm.Items.Item("8").Specific
            'alldataSource(oForm)

            'Dim strItemCode As String = oDBDataSourceLines.GetValue("U_ItemCode", 0).Trim()
            'Dim strItemName As String = oDBDataSourceLines.GetValue("U_ItemName", 0).Trim()

            'If strItemCode.Trim().Length = 0 Then
            '    oApplication.Utilities.Message("Enter Item Code...", SAPbouiCOM.BoStatusBarMessageType.smt_Error)
            '    Return False
            '    'ElseIf strItemName.Trim().Length = 0 Then
            '    '    oApplication.Utilities.Message("Enter Item Name...", SAPbouiCOM.BoStatusBarMessageType.smt_Error)
            '    '    Return False
            'End If

            'oMatrix = oForm.Items.Item("11").Specific
            'For index As Integer = 1 To oMatrix.VisualRowCount
            '    Dim strCode As String = oApplication.Utilities.getMatrixValues(oMatrix, "V_1", index)
            '    For intRow As Integer = 1 To oMatrix.VisualRowCount
            '        If index <> intRow Then
            '            Dim strCode1 As String = oApplication.Utilities.getMatrixValues(oMatrix, "V_1", intRow)
            '            If strCode = strCode1 Then
            '                oApplication.Utilities.Message("Item Code Already Exist... " + strCode1.ToString(), SAPbouiCOM.BoStatusBarMessageType.smt_Error)
            '                Return False
            '            End If
            '        End If
            '    Next
            'Next
            oRecordSet = oApplication.Company.GetBusinessObject(BoObjectTypes.BoRecordset)
            If oForm.Mode = SAPbouiCOM.BoFormMode.fm_ADD_MODE Then
                strQuery = "Select 1 As 'Return',DocEntry From [@Z_HDF_OBND]"
                strQuery += " Where "
                strQuery += " U_Type = '" + oDBDataSource.GetValue("U_Type", 0).Trim() + "' "
               

            ElseIf oForm.Mode = SAPbouiCOM.BoFormMode.fm_UPDATE_MODE Then
                strQuery = "Select 1 As 'Return',DocEntry From [@Z_HDF_OBND]"
                strQuery += " Where "
                strQuery += " U_Type = '" + oDBDataSource.GetValue("U_Type", 0).Trim() + "' "
                strQuery += " And DocEntry <> '" + oDBDataSource.GetValue("DocEntry", 0).ToString() + "'"
            End If
            oRecordSet.DoQuery(strQuery)
            If Not oRecordSet.EoF Then
                oApplication.Utilities.Message("Type Already Exists ...", SAPbouiCOM.BoStatusBarMessageType.smt_Error)
                Return False
            End If
            Return True
        Catch ex As Exception
            aForm.Freeze(False)
            Throw ex
        End Try
    End Function


End Class

