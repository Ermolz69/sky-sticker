namespace SkySticker.Dialogs;

public class RenameDialog : Form
{
    private TextBox _textBox = null!;
    private string _newName = "";
    public string NewName => _newName;

    public RenameDialog(string currentName)
    {
        this.Text = "Rename";
        this.Size = new Size(300, 120);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        _textBox = new TextBox
        {
            Text = currentName,
            Location = new Point(12, 12),
            Size = new Size(260, 23),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        var btnOk = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(116, 50),
            Size = new Size(75, 23)
        };
        btnOk.Click += (s, e) =>
        {
            _newName = _textBox.Text;
            this.DialogResult = DialogResult.OK;
        };

        var btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(197, 50),
            Size = new Size(75, 23)
        };

        this.Controls.Add(_textBox);
        this.Controls.Add(btnOk);
        this.Controls.Add(btnCancel);
        this.AcceptButton = btnOk;
        this.CancelButton = btnCancel;
    }
}

