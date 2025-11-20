using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PdfiumOverlayTest
{
    partial class CategoriesForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
                // ContextMenu aufräumen
                _contextMenu?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            _lstTags = new ListBox();
            _buttonPanel = new Panel();
            _btnAdd = new Button();
            _btnEdit = new Button();
            _btnDelete = new Button();
            _btnClose = new Button();
            _buttonPanel.SuspendLayout();
            SuspendLayout();
            // 
            // _lstTags
            // 
            _lstTags.DrawMode = DrawMode.OwnerDrawFixed;
            _lstTags.Font = new Font("Segoe UI", 10F);
            _lstTags.ItemHeight = 50;
            _lstTags.Location = new Point(14, 14);
            _lstTags.Margin = new Padding(4, 3, 4, 3);
            _lstTags.Name = "_lstTags";
            _lstTags.Size = new Size(419, 454);
            _lstTags.TabIndex = 0;
            _lstTags.DrawItem += LstTags_DrawItem;
            _lstTags.DoubleClick += LstTags_DoubleClick;
            // 
            // _buttonPanel
            // 
            _buttonPanel.BackColor = Color.FromArgb(240, 240, 240);
            _buttonPanel.Controls.Add(_btnAdd);
            _buttonPanel.Controls.Add(_btnEdit);
            _buttonPanel.Controls.Add(_btnDelete);
            _buttonPanel.Controls.Add(_btnClose);
            _buttonPanel.Location = new Point(14, 487);
            _buttonPanel.Margin = new Padding(4, 3, 4, 3);
            _buttonPanel.Name = "_buttonPanel";
            _buttonPanel.Size = new Size(420, 162);
            _buttonPanel.TabIndex = 1;
            // 
            // _btnAdd
            // 
            _btnAdd.BackColor = Color.FromArgb(235, 235, 235);
            _btnAdd.FlatStyle = FlatStyle.Flat;
            _btnAdd.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _btnAdd.Location = new Point(12, 14);
            _btnAdd.Margin = new Padding(4, 3, 4, 3);
            _btnAdd.Name = "_btnAdd";
            _btnAdd.Size = new Size(117, 42);
            _btnAdd.TabIndex = 0;
            _btnAdd.Text = "Hinzufügen";
            _btnAdd.UseVisualStyleBackColor = false;
            _btnAdd.Click += BtnAdd_Click;
            // 
            // _btnEdit
            // 
            _btnEdit.BackColor = Color.FromArgb(235, 235, 235);
            _btnEdit.FlatStyle = FlatStyle.Flat;
            _btnEdit.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _btnEdit.Location = new Point(152, 14);
            _btnEdit.Margin = new Padding(4, 3, 4, 3);
            _btnEdit.Name = "_btnEdit";
            _btnEdit.Size = new Size(117, 42);
            _btnEdit.TabIndex = 1;
            _btnEdit.Text = "Ändern";
            _btnEdit.UseVisualStyleBackColor = false;
            _btnEdit.Click += BtnEdit_Click;
            // 
            // _btnDelete
            // 
            _btnDelete.BackColor = Color.FromArgb(235, 235, 235);
            _btnDelete.FlatStyle = FlatStyle.Flat;
            _btnDelete.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _btnDelete.Location = new Point(292, 14);
            _btnDelete.Margin = new Padding(4, 3, 4, 3);
            _btnDelete.Name = "_btnDelete";
            _btnDelete.Size = new Size(117, 42);
            _btnDelete.TabIndex = 2;
            _btnDelete.Text = "Löschen";
            _btnDelete.UseVisualStyleBackColor = false;
            _btnDelete.Click += BtnDelete_Click;
            // 
            // _btnClose
            // 
            _btnClose.BackColor = Color.FromArgb(235, 235, 235);
            _btnClose.FlatStyle = FlatStyle.Flat;
            _btnClose.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _btnClose.Location = new Point(12, 86);
            _btnClose.Margin = new Padding(4, 3, 4, 3);
            _btnClose.Name = "_btnClose";
            _btnClose.Size = new Size(397, 46);
            _btnClose.TabIndex = 3;
            _btnClose.Text = "Beenden";
            _btnClose.UseVisualStyleBackColor = false;
            _btnClose.Click += BtnClose_Click;
            // 
            // CategoriesForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(448, 661);
            Controls.Add(_buttonPanel);
            Controls.Add(_lstTags);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4, 3, 4, 3);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CategoriesForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Kategorien (Tags) verwalten";
            _buttonPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.ListBox _lstTags;
        private System.Windows.Forms.Panel _buttonPanel;
        private System.Windows.Forms.Button _btnAdd;
        private System.Windows.Forms.Button _btnEdit;
        private System.Windows.Forms.Button _btnDelete;
        private System.Windows.Forms.Button _btnClose;
    }
}