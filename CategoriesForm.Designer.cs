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
            this._lstTags = new System.Windows.Forms.ListBox();
            this._buttonPanel = new System.Windows.Forms.Panel();
            this._btnAdd = new System.Windows.Forms.Button();
            this._btnEdit = new System.Windows.Forms.Button();
            this._btnDelete = new System.Windows.Forms.Button();
            this._btnClose = new System.Windows.Forms.Button();
            this._buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _lstTags
            // 
            this._lstTags.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this._lstTags.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._lstTags.ItemHeight = 50;
            this._lstTags.Location = new System.Drawing.Point(12, 12);
            this._lstTags.Name = "_lstTags";
            this._lstTags.Size = new System.Drawing.Size(360, 404);
            this._lstTags.TabIndex = 0;
            this._lstTags.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.LstTags_DrawItem);
            this._lstTags.DoubleClick += new System.EventHandler(this.LstTags_DoubleClick);
            // 
            // _buttonPanel
            // 
            this._buttonPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this._buttonPanel.Controls.Add(this._btnAdd);
            this._buttonPanel.Controls.Add(this._btnEdit);
            this._buttonPanel.Controls.Add(this._btnDelete);
            this._buttonPanel.Controls.Add(this._btnClose);
            this._buttonPanel.Location = new System.Drawing.Point(12, 422);
            this._buttonPanel.Name = "_buttonPanel";
            this._buttonPanel.Size = new System.Drawing.Size(360, 110);
            this._buttonPanel.TabIndex = 1;
            // 
            // _btnAdd
            // 
            this._btnAdd.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(235)))), ((int)(((byte)(235)))));
            this._btnAdd.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnAdd.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._btnAdd.Location = new System.Drawing.Point(10, 12);
            this._btnAdd.Name = "_btnAdd";
            this._btnAdd.Size = new System.Drawing.Size(100, 36);
            this._btnAdd.TabIndex = 0;
            this._btnAdd.Text = "Hinzufügen";
            this._btnAdd.UseVisualStyleBackColor = false;
            this._btnAdd.Click += new System.EventHandler(this.BtnAdd_Click);
            // 
            // _btnEdit
            // 
            this._btnEdit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(235)))), ((int)(((byte)(235)))));
            this._btnEdit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnEdit.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._btnEdit.Location = new System.Drawing.Point(130, 12);
            this._btnEdit.Name = "_btnEdit";
            this._btnEdit.Size = new System.Drawing.Size(100, 36);
            this._btnEdit.TabIndex = 1;
            this._btnEdit.Text = "Ändern";
            this._btnEdit.UseVisualStyleBackColor = false;
            this._btnEdit.Click += new System.EventHandler(this.BtnEdit_Click);
            // 
            // _btnDelete
            // 
            this._btnDelete.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(235)))), ((int)(((byte)(235)))));
            this._btnDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnDelete.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._btnDelete.Location = new System.Drawing.Point(250, 12);
            this._btnDelete.Name = "_btnDelete";
            this._btnDelete.Size = new System.Drawing.Size(100, 36);
            this._btnDelete.TabIndex = 2;
            this._btnDelete.Text = "Löschen";
            this._btnDelete.UseVisualStyleBackColor = false;
            this._btnDelete.Click += new System.EventHandler(this.BtnDelete_Click);
            // 
            // _btnClose
            // 
            this._btnClose.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(235)))), ((int)(((byte)(235)))));
            this._btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnClose.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this._btnClose.Location = new System.Drawing.Point(10, 60);
            this._btnClose.Name = "_btnClose";
            this._btnClose.Size = new System.Drawing.Size(340, 40);
            this._btnClose.TabIndex = 3;
            this._btnClose.Text = "Beenden";
            this._btnClose.UseVisualStyleBackColor = false;
            this._btnClose.Click += new System.EventHandler(this.BtnClose_Click);
            // 
            // CategoriesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 544);
            this.Controls.Add(this._buttonPanel);
            this.Controls.Add(this._lstTags);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CategoriesForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Kategorien (Tags) verwalten";
            this._buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);
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