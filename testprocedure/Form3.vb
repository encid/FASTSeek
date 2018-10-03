Public Class Form3
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If My.Forms.Form1.cmdSearch.Enabled = True Then Me.Hide()
    End Sub
End Class