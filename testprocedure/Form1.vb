'FAST Test Procedure Search
'written by R. Cavallaro

Imports System.IO
Imports System.Data.OleDb
Imports System.Text.RegularExpressions
Imports System.ComponentModel

Public Class Form1
    'dim provider As String = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source="                  'ACE OLEDB provider, disabled
    Dim provider As String = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source="                    'data provider
    Dim dbPath As String = "\\ares\shared\Operations\Test Engineering\FASTSeek\"                 'path of IID (ProductCode) mdb file
    Dim iniFile As String = dbPath & "\settings.tp"
    Dim vaultPath As String = "\\pandora\vault\Released_Part_Information"
    Dim appPath As String = My.Application.Info.DirectoryPath
    Dim prod, connString, hms As String
    Public myConnection As New OleDbConnection
    Public dr As OleDbDataReader
    Dim fileDate As New Date

    Private Function iidFile(ByVal iniFile As String) As String
        Dim lff As String = Nothing

        'open settings.ini file. which has the path of the database file
        Try
            Using sr As StreamReader = New StreamReader(iniFile)
                While Not sr.EndOfStream
                    lff = sr.ReadLine
                End While
                sr.Close()
            End Using
        Catch ex As Exception
            MsgBox(ex.Message & vbCrLf & vbCrLf & "Please create this file and add the database file path to the first line.")
            Me.Close()
        End Try

        Return lff     'set database file
    End Function

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim form3 As New Form3
        form3.Hide()
        CheckForIllegalCrossThreadCalls = False
        txtDbFile.Text = iidFile(iniFile)
        writeLog("Program started.  Ready to search")
        cboSerial.Select()
    End Sub

    Private Sub cmdOpen_Click(sender As Object, e As EventArgs) Handles cmdOpen.Click
        Try
            'make sure listbox is populated
            If lstProc.Items.Count > 0 Then
                Dim f As New FileInfo(lstProc.SelectedItem.ToString)
                Dim lcasef As String = LCase(f.ToString)
                If lcasef.Contains("ready for release") Then
                    MessageBox.Show("Note:  This procedure has not yet been released to the vault.", "(FAST.)Seek", MessageBoxButtons.OK)
                End If
                If lblArchived.Visible Then
                    If MessageBox.Show("This procedure is archived, and may not be the newest revision." & vbCrLf & vbCrLf &
                                       "Are you sure you want to open this procedure?", "(FAST.)Seek", MessageBoxButtons.YesNo) =
                                       DialogResult.No Then Exit Sub
                End If
                Try
                    'open the procedure file
                    WriteLog("Opening test procedure " + lstProc.SelectedItem.ToString)
                    Process.Start(CType(lstProc.SelectedItem, IO.FileInfo).FullName)
                Catch ex As Exception
                    MsgBox("An error has occured!")
                End Try
            Else
                WriteLog("No procedure selected, please enter serial number")
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Sub ListBox1_DoubleClick(sender As Object, e As EventArgs) Handles lstProc.DoubleClick
        'open procedure file process
        Call cmdOpen_Click(sender, e)
    End Sub

    'Private Sub TextBox1_KeyPress(sender As Object, e As KeyPressEventArgs)
    'If e.KeyChar = ChrW(Keys.Return) Then
    '        Call Button1_Click(sender, e)
    '    End If
    'End Sub

    Private Sub ListBox1_KeyPress(sender As Object, e As KeyPressEventArgs) Handles lstProc.KeyPress
        If e.KeyChar = ChrW(Keys.Return) Then
            If lstProc.Items.Count > 0 Then
                Call cmdOpen_Click(sender, e)
            End If
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles cmdSearch.Click
        If BackgroundWorker1.IsBusy Then Exit Sub
        cmdSearch.Enabled = False
        Dim form3 As New Form3
        form3.Show()
        If Not cboSerial.Items.Contains(cboSerial.Text) Then cboSerial.Items.Add(cboSerial.Text)
        BackgroundWorker1.RunWorkerAsync()
    End Sub

    Public Sub writeIni(path As String)
        Dim fn As String = dbPath & "\settings.tp"

        Using sw As StreamWriter = New StreamWriter(fn)
            sw.WriteLine(path)
            sw.Close()
        End Using

    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click

        Dim openFileDialog1 As New OpenFileDialog()
        Dim dbFileName As String
        Dim dbExtension As String

        'set dialog parameters
        With openFileDialog1
            .InitialDirectory = "\\ares\Shared\Operations\Test Engineering\FASTSeek\"
            .Filter = "Access Database MDB files (*.mdb)|*.mdb|All Files|*.*"
            .FilterIndex = 1
            .RestoreDirectory = False
        End With

        'write new path to IID database to line 1 of settings.ini
        If openFileDialog1.ShowDialog() = System.Windows.Forms.DialogResult.OK Then
            dbFileName = openFileDialog1.FileName
            dbExtension = dbFileName.Substring(Len(dbFileName) - 4, 4)
            If dbExtension = ".mdb" Then
                Call writeIni(dbFileName)
            Else
                MessageBox.Show("File must be an Access Database (.mdb) file.  Please Try again.")
            End If
        End If
    End Sub

    Private Sub Button5_Click_1(sender As Object, e As EventArgs) Handles Button5.Click
        MessageBox.Show("Database path is stored in the file 'settings.ini' in this application's folder." + vbCr + vbCr + "WARNING!  Do not change unless you are instructed to by your supervisor or IT.")
    End Sub

    Private Sub Form1_KeyPress(sender As Object, e As KeyPressEventArgs) Handles Me.KeyPress
        If cboProduct.Focused Or ComboBox1.Focused Then
            Exit Sub
        End If
        If lstProc.Focused = True Then
            If e.KeyChar = ChrW(Keys.Return) Then
                cmdOpen_Click(sender, e)
                e.Handled = True
                Exit Sub
            End If
        End If

        If cboSerial.Focused = False Then
            If e.KeyChar = "[" Or e.KeyChar = "]" Then e.KeyChar = ""
            cboSerial.Focus()
            cboSerial.Text = e.KeyChar.ToString
            cboSerial.SelectionStart = cboSerial.Text.Length
            e.Handled = True
        End If

        Select Case e.KeyChar
            Case "["
                cboSerial.SelectAll()
                e.KeyChar = ""
            Case "]"
                Call Button1_Click(sender, e)
                e.KeyChar = ""
        End Select
    End Sub

    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Label5.Text = TimeOfDay.ToString("h:mm:ss tt")
    End Sub

    Private Sub findProcedures(ByVal serialNumber As String)
        Dim serial, str, aNum, IID, prodFirst3 As String
        Dim gotFile As Boolean = False
        Dim rx As New Regex("[^a-zA-Z0-9]")

        Try
            'clear textboxes
            lstProc.Items.Clear()
            txtProc.Clear()
            txtProd.Clear()
            txtDate.Clear()

            'trim non-alphanumeric characters
            serial = rx.Replace(serialNumber, "")

            'On Error Resume Next

            'check length of barcode to determine validity
            If serial = "" Then
                writeLog("Please scan or enter a serial number")
                cboSerial.Focus()
                cboSerial.SelectAll()
                Exit Sub
            End If
            If serial.Length = 1 Or serial.Length = 2 Then
                writeLog("Invalid serial number: " & serialNumber)
                cboSerial.Focus()
                cboSerial.SelectAll()
                Exit Sub
            End If

            'get IID from serial numbers
            IID = serial.Substring(0, 3)

            'open database and using the IID from serial number, find corresponding product number
            connString = provider & iidFile(iniFile)
            myConnection.ConnectionString = connString
            myConnection.Open()
            str = "SELECT * FROM ProductCode WHERE (ProductID = '" & IID & "')"
            Dim cmd As OleDbCommand = New OleDbCommand(str, myConnection)
            dr = cmd.ExecuteReader

            While dr.Read()
                If dr("Product").ToString <> "" Then        'if procedure exists, assign product number to string prod
                    prod = dr("Product").ToString
                    txtProd.Text = prod
                Else
                    writeLog("Invalid serial number: " & serialNumber)
                    cboSerial.Focus()
                    cboSerial.SelectAll()
                    myConnection.Close()
                    Exit Sub
                End If
            End While
            myConnection.Close()

            'get first 3 digits of product number to determine if final assy or sub assy
            prodFirst3 = prod.Substring(0, 3)

            'remove first 4 characters of product number to get assembly number
            aNum = prod.Remove(0, 4)

            'find procedure based on first 3 digits
            Select Case prodFirst3
                Case "231"
                    'search each subdirectory in \234 for a test procedure match
                    For Each currentFile As String In Directory.EnumerateFiles("\\pandora\vault\Released_Part_Information\234-xxxxx_Assy_Test_Proc\Standard_Products\",
                                                                                 "*" & aNum & "*", SearchOption.AllDirectories)
                        Dim f As New FileInfo(currentFile)
                        'check for duplicate files in listbox, and if no duplicates exist, add the file
                        'Also, check for archive, and do NOT add archived files to list.
                        If lstProc.FindStringExact(Path.GetFileName(f.FullName)) = -1 And LCase(f.FullName).Contains("archive") = False Then lstProc.Items.Add(f)
                        If lstProc.Items.Count > 0 Then gotFile = True
                    Next
                    If gotFile = True Then
                        writeLog("Found procedure for [" + IID + "] product number " + prod)
                        txtProd.Text = prod
                        cboSerial.SelectAll()
                        lstProc.SelectedIndex = 0
                        lstProc.Focus()
                    ElseIf gotFile = False Then
                        For Each currentFile As String In Directory.EnumerateFiles("\\ares\shared\Operations\Test Engineering\Documents ready for release",
                                                                                   "*" & aNum & "*", SearchOption.TopDirectoryOnly)
                            Dim f As New FileInfo(currentFile)
                            'check for duplicate files in listbox, and if no duplicates exist, add the file
                            If lstProc.FindStringExact(Path.GetFileName(f.FullName)) = -1 Then lstProc.Items.Add(f)
                            gotFile = True
                        Next
                        If gotFile = True Then
                            WriteLog(String.Format("Found non-released procedure for [{0}] product number {1}", IID, prod))
                            txtProd.Text = prod
                            cboSerial.SelectAll()
                            lstProc.SelectedIndex = 0
                            lstProc.Focus()
                        ElseIf gotFile = False Then
                            writeLog("Procedure not found for [" + IID + "] product number " + prod)
                            cboSerial.Focus()
                            cboSerial.SelectAll()
                        End If
                    End If
                Case "216"
                    'search each subdirectory in \225 for a test procedure match
                    For Each currentFile As String In Directory.EnumerateFiles("\\pandora\vault\Released_Part_Information\225-xxxxx_Proc_Mfg_Test",
                                                                               "*" & aNum & "*", SearchOption.AllDirectories)
                        Dim f As New FileInfo(currentFile)
                        'check for duplicate files in listbox, and if no duplicates exist, add the file
                        'Also, check for archive, and do NOT add archived files to list.
                        If lstProc.FindStringExact(Path.GetFileName(f.FullName)) = -1 And LCase(f.FullName).Contains("archive") = False Then lstProc.Items.Add(f)
                        If lstProc.Items.Count > 0 Then gotFile = True
                    Next
                    If gotFile = True Then
                        writeLog("Found procedure for [" + IID + "] product number " + prod)
                        txtProd.Text = prod
                        cboSerial.SelectAll()
                        lstProc.SelectedIndex = 0
                        lstProc.Focus()
                    ElseIf gotFile = False Then
                        For Each currentFile As String In Directory.EnumerateFiles("\\ares\shared\Operations\Test Engineering\Documents ready for release",
                                                                                   "*" & aNum & "*", SearchOption.TopDirectoryOnly)
                            Dim f As New FileInfo(currentFile)
                            'check for duplicate files in listbox, and if no duplicates, add the file
                            If lstProc.FindStringExact(Path.GetFileName(f.FullName)) = -1 Then lstProc.Items.Add(f)
                            gotFile = True
                        Next
                        If gotFile = True Then
                            writeLog("Found non-released procedure for [" + IID + "] product number " + prod)
                            txtProd.Text = prod
                            cboSerial.SelectAll()
                            lstProc.SelectedIndex = 0
                            lstProc.Focus()
                        ElseIf gotFile = False Then
                            writeLog("Procedure not found for [" + IID + "] product number " + prod)
                            cboSerial.Focus()
                            cboSerial.SelectAll()
                        End If
                    End If
                Case Else
                    writeLog("Procedure not found for [" + IID + "] product number " + prod)
                    cboSerial.Focus()
                    cboSerial.SelectAll()
                    Exit Sub
Errorcatch:
                    writeLog("An error has occured")
                    cboSerial.Focus()
                    cboSerial.SelectAll()
            End Select
        Catch ex As Exception
            Exit Sub
        End Try
    End Sub

    Private Sub writeLog(ByVal logText As String)
        hms = String.Format("{0:hh:mm:sstt}", System.DateTime.Now)   'writes time into var hms
        If txtLog.Text = "" Then
            txtLog.AppendText(hms + ">  " + logText + ".")
        Else
            txtLog.AppendText(vbCrLf + hms + ">  " + logText + ".")
        End If

        Using w As StreamWriter = File.AppendText(dbPath + "\log.txt")
            LogToFile(logText, w)
        End Using
    End Sub

    Private Sub LogToFile(logMessage As String, w As TextWriter)
        Dim dateStr, compNameStr, loginStr As String

        dateStr = String.Format("{0} {1}", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString())
        loginStr = Security.Principal.WindowsIdentity.GetCurrent().Name
        compNameStr = Environment.MachineName

        w.WriteLine("{0}, {1}, {2}, {3}", dateStr, compNameStr, loginStr, logMessage)
    End Sub

    Private Sub BackgroundWorker1_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork
        cmdSearch.Enabled = False
        Call findProcedures(cboSerial.Text)
    End Sub

    Private Sub lstProc_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lstProc.SelectedIndexChanged
        'check if listbox has items
        If lstProc.Items.Count > 0 Then
            Dim f As New FileInfo(lstProc.SelectedItem.ToString)
            Dim lcasef As String = LCase(f.ToString)

            'trim extension from filename and display it in textbox as procedure name
            txtProc.Text = Path.GetFileNameWithoutExtension(lstProc.SelectedItem.ToString)
            fileDate = File.GetLastWriteTime(CType(lstProc.SelectedItem, IO.FileInfo).FullName)
            txtDate.Text = fileDate.ToString("MM/dd/yyyy")

            If lcasef.Contains("archive") Then
                lblArchived.Visible = True
            Else
                lblArchived.Visible = False
            End If
        End If
    End Sub

    Private Sub Label6_DoubleClick(sender As Object, e As EventArgs) Handles Label6.DoubleClick
        Dim form2 As New Form2
        form2.ShowDialog()
    End Sub

    Private Sub BackgroundWorker1_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles BackgroundWorker1.RunWorkerCompleted
        cmdSearch.Enabled = True
    End Sub

    Private Sub Button1_Click_1(sender As Object, e As EventArgs) Handles Button1.Click
        Dim str, productNum, productID As String
        Dim myCon As New OleDbConnection
        Dim myDr As OleDbDataReader

        'get product number from combobox
        productNum = cboProduct.Text

        'open database and using the product number, find corresponding IID
        connString = provider & iidFile(iniFile)
        myCon.ConnectionString = connString
        myCon.Open()
        str = "SELECT * FROM ProductCode WHERE (Product = '" & productNum & "')"
        Dim cmd As OleDbCommand = New OleDbCommand(str, myCon)
        myDr = cmd.ExecuteReader

        While myDr.Read()
            If myDr("ProductID").ToString <> "" Then        'if procedure exists, assign product number to string prod
                productID = myDr("ProductID").ToString
                txtIID.Text = productID
            Else
                WriteLog("Invalid serial number: " & productNum)
                cboProduct.Focus()
                cboProduct.SelectAll()
                myConnection.Close()
                Exit Sub
            End If
        End While
        myConnection.Close()
    End Sub

    Private Sub Button2_Click_1(sender As Object, e As EventArgs) Handles Button2.Click
        Dim pn As String = ComboBox1.Text
        Dim pnSubstring As String
        Dim dir As DirectoryInfo

        ListBox1.Items.Clear()
        'error checking
        If pn = "" Then Exit Sub

        Try
            pnSubstring = pn.Substring(0, 3)  'get product substring to determine directory
            'determine the type of assembly based on first 3 digits, and fill dir variable
            Select Case pnSubstring
                Case "213"
                    dir = New DirectoryInfo("\\pandora\vault\Operations_Documents\PROCESS SHEETS\213-xxxxx_Assy_Mech\" + pn.Substring(0, 9))
                Case "216"
                    dir = New DirectoryInfo("\\pandora\vault\Operations_Documents\PROCESS SHEETS\216-xxxxx_PCB_Assy_Part_List\" + pn.Substring(0, 9))
                Case "221"
                    dir = New DirectoryInfo("\\pandora\vault\Operations_Documents\PROCESS SHEETS\221-xxxxx_Internal_Harness\" + pn.Substring(0, 9))
                Case "222"
                    dir = New DirectoryInfo("\\pandora\vault\Operations_Documents\PROCESS SHEETS\222-xxxxx_Extnl_Harness_Cable\" + pn.Substring(0, 9))
                Case "230"
                    dir = New DirectoryInfo("\\pandora\vault\Operations_Documents\PROCESS SHEETS\230-xxxxx_Modified_Altered_Items\" + pn.Substring(0, 9))
                Case "231"
                    dir = New DirectoryInfo("\\pandora\vault\Operations_Documents\PROCESS SHEETS\231-xxxxx_Shipping_Final_Assy\" + pn.Substring(0, 9))
                Case "233"
                    dir = New DirectoryInfo("\\pandora\vault\Operations_Documents\PROCESS SHEETS\233-xxxxx_Final_Comp_Assy\" + pn.Substring(0, 9))
                Case Else
                    Exit Sub
            End Select
            'get all pdfs matching part number
            Dim files = dir.EnumerateFiles("*" & pn & "*" + ".pdf", SearchOption.AllDirectories)
            If files.Any() = False Then
                files = dir.EnumerateFiles(pn.Substring(0, 9) & "-XX" & "*" + ".pdf", SearchOption.AllDirectories)
            End If
            If files.Any() = False Then
                files = dir.EnumerateFiles(pn.Substring(0, 9) & "-ALL" & "*" + ".pdf", SearchOption.AllDirectories)
            End If
            Dim file = files.OrderByDescending(Function(f) f.LastWriteTime).FirstOrDefault

            'check for duplicate files in listbox, and if no duplicates exist, add the file
            'Also, check for archive, and do NOT add archived files to list.
            If file IsNot Nothing Then
                If ListBox1.FindStringExact(Path.GetFileName(file.FullName)) = -1 And LCase(file.FullName).Contains("archive") = False And file.FullName.EndsWith(".pdf") Then
                    ListBox1.Items.Add(file)
                    If Not ComboBox1.Items.Contains(ComboBox1.Text) Then ComboBox1.Items.Insert(0, ComboBox1.Text)
                End If
            End If
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub

    Private Sub cboSerial_KeyPress(sender As Object, e As KeyPressEventArgs) Handles cboSerial.KeyPress
        If e.KeyChar = ChrW(Keys.Return) Then
            Call Button1_Click(sender, e)
        End If
        If Char.IsLetter(e.KeyChar) Then
            e.KeyChar = Char.ToUpper(e.KeyChar)
        End If
    End Sub

    Private Sub Form1_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        WriteLog("Exiting program")
    End Sub
End Class