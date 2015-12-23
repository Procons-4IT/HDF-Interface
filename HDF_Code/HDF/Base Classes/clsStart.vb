Imports System.IO

Public Class clsStart

    Shared Sub Main()
        'Dim i As Integer
        Dim oMenuItem As SAPbouiCOM.MenuItem
        Try
            Try
                oApplication = New clsListener
                oApplication.Utilities.Connect()
                oApplication.SetFilter()

                With oApplication.Company.GetCompanyService
                    CompanyDecimalSeprator = .GetAdminInfo.DecimalSeparator
                    CompanyThousandSeprator = .GetAdminInfo.ThousandsSeparator
                End With

            Catch ex As Exception
                MessageBox.Show(ex.Message, "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly)
                Exit Sub
            End Try
            oApplication.Utilities.CreateTables()
            oApplication.Utilities.AddRemoveMenus("Menu.xml")
            oMenuItem = oApplication.SBO_Application.Menus.Item("mnu_S301")
            oMenuItem.Image = Application.StartupPath & "\Logo.PNG"

            Dim strPath As String = System.Windows.Forms.Application.StartupPath & "\Script\Script.txt"
            Dim strQuery As String = File.ReadAllText(strPath)
            Dim oRec_ExeSP As SAPbobsCOM.Recordset = oApplication.Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset)
            oRec_ExeSP.DoQuery(strQuery)

            oApplication.Utilities.Message("HDF Addon Connected Successfully..", SAPbouiCOM.BoStatusBarMessageType.smt_Success)

            oApplication.Utilities.addUDODefaultValues()

            oApplication.Utilities.NotifyAlert()
            System.Windows.Forms.Application.Run()

        Catch ex As Exception
            oApplication.Utilities.Message(ex.Message, SAPbouiCOM.BoStatusBarMessageType.smt_Error)
        End Try

    End Sub

End Class
