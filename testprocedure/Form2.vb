Public Class Form2
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If TextBox1.Text = "fastfast" Then
            My.Forms.Form1.GroupBox3.Enabled = True
            My.Forms.Form1.GroupBox3.Visible = True
            Me.Close()
            Exit Sub
        End If
        MessageBox.Show("Invalid password.", "(FAST.) Test Procedure Search")
        TextBox1.SelectAll()
        TextBox1.Focus()
    End Sub

    Private Sub TextBox1_KeyPress(sender As Object, e As KeyPressEventArgs) Handles TextBox1.KeyPress
        If e.KeyChar = vbCr Then Call Button1_Click(TextBox1, e)
    End Sub

    Private Sub Form2_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyCode = Keys.Escape Then
            Me.Close()
        End If
    End Sub
End Class