using System;
using System.Drawing;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;
using DarkTheme.Properties;
using KeePass;
using KeePass.App;
using KeePass.UI;
using KeePass.UI.ToolStripRendering;

namespace DarkTheme
{
	internal class DarkTheme
	{
		private ToolStripRenderer _defaultRenderer;
		private ToolStripRenderer _darkModeRenderer;
		
		private IControlStyle _controlStyle;
		private bool _enabled;

		public bool Enabled
		{
			get { return _enabled; }
			set { SetEnable(value); }}

		public DarkTheme(bool enabled)
		{
			_defaultRenderer = ToolStripManager.Renderer;
			_darkModeRenderer = new ProExtTsr(new DarkThemeColorTable());

			SetEnable(enabled);
		}

		private void SetEnable(bool enable)
		{
			_enabled = enable;

			_controlStyle = _enabled ? (IControlStyle) new DarkStyle() : new DefaultStyle();
			
			ToolStripManager.Renderer = _enabled ? _darkModeRenderer : _defaultRenderer;

			var colorControlNormalField =
				typeof(AppDefs).GetField("ColorControlNormal", BindingFlags.Static | BindingFlags.Public);
			var colorControlDisabledField =
				typeof(AppDefs).GetField("ColorControlDisabled", BindingFlags.Static | BindingFlags.Public);

			if (colorControlNormalField != null)
				colorControlNormalField.SetValue(null, _controlStyle.NormalField);
			
			if (colorControlDisabledField != null)
				colorControlDisabledField.SetValue(null, _controlStyle.DisabledField);
			
		}

		public void Apply(Control control)
		{
			control.BackColor = _controlStyle.BackColor;
			control.ForeColor = _controlStyle.ForeColor;
			
			if (control is Form form) Apply(form);
			if (control is Button button) Apply(button);
			if (control is TreeView treeView) Apply(treeView);
			if (control is RichTextBox richTextBox) Apply(richTextBox);
			if (control is LinkLabel linkLabel) Apply(linkLabel);
			if (control is ListView listView) Apply(listView);
			if (control is SecureTextBoxEx secureTextBoxEx) Apply(secureTextBoxEx);
		}

		private void Apply(SecureTextBoxEx secureTextBoxEx)
		{
			secureTextBoxEx.BackColorChanged -= HandleSecureTextBoxExOnBackColorChanged;
			secureTextBoxEx.BackColorChanged += HandleSecureTextBoxExOnBackColorChanged;
		}

		private void HandleSecureTextBoxExOnBackColorChanged(object sender, EventArgs e)
		{
			if (!_enabled)
			{
				return;
			}
			
			var textBox = (SecureTextBoxEx) sender;
			if (textBox.BackColor == SystemColors.Window)
				textBox.BackColor = Colors.Control;
		}

		private void Apply(Form form)
		{
			form.BackColor = _controlStyle.BackColor;
			form.ForeColor = _controlStyle.ForeColor;
		}

		private void Apply(Button button)
		{
			button.FlatAppearance.BorderColor = _controlStyle.ButtonBorderColor;
			button.FlatStyle = _controlStyle.ButtonFlatStyle;
		}

		private void Apply(LinkLabel linkLabel)
		{
			linkLabel.LinkColor = _controlStyle.LinkColor;
		}

		private void Apply(TreeView treeView)
		{
			treeView.BorderStyle = _controlStyle.BorderStyle;
			treeView.BackColor = _controlStyle.TreeViewBackColor;
			treeView.DrawMode = _controlStyle.TreeViewDrawMode;

			treeView.DrawNode -= HandleTreeViewDrawNode;
			treeView.DrawNode += HandleTreeViewDrawNode;
		}

		private void HandleTreeViewDrawNode(object sender, DrawTreeNodeEventArgs e)
		{
			e.DrawDefault = true;
			e.Node.ForeColor = e.State == TreeNodeStates.Selected ? Colors.WindowText : Colors.ControlText;
		}

		private void Apply(RichTextBox richTextBox)
		{
			richTextBox.BorderStyle = _controlStyle.BorderStyle;
			richTextBox.TextChanged -= HandleRichTextBoxTextChanged;
			richTextBox.TextChanged += HandleRichTextBoxTextChanged;
		}

		private void HandleRichTextBoxTextChanged(object sender, EventArgs e)
		{
			var richTextBox = (RichTextBox) sender;
			var selectionStart = richTextBox.SelectionStart;
			var selectionLength = richTextBox.SelectionLength;

			richTextBox.SelectAll();
			richTextBox.SelectionColor = _controlStyle.ForeColor;
			richTextBox.Select(selectionStart, selectionLength);
		}

		private void Apply(ListView listView)
		{
			if (!listView.OwnerDraw)
			{
				listView.OwnerDraw = true;

				listView.DrawColumnHeader += HandleListViewDrawColumnHeader;
				listView.DrawItem += HandleListViewDrawItem;
				listView.DrawSubItem += HandleListViewDrawSubItem;
			}

			listView.BorderStyle = _controlStyle.BorderStyle;
			listView.BackColor = _controlStyle.ListViewBackColor;
			listView.BackgroundImage = _controlStyle.ListViewBackground;
			listView.BackgroundImageTiled = _controlStyle.ListViewBackgroundTiled;
		}

		private void HandleListViewDrawItem(object sender, DrawListViewItemEventArgs e)
		{
			if (!_enabled)
			{
				e.DrawDefault = true;
				return;
			}

			if (e.State == 0)
			{
				e.DrawFocusRectangle();
				return;
			}

			var backColor = Program.Config.MainWindow.EntryListAlternatingBgColors && (e.Item.Index & 1) == 0
				? Colors.Window
				: Colors.LightWindow;

			using (var brush = new SolidBrush(backColor))
			{
				e.Graphics.FillRectangle(brush, e.Bounds);
			}

			e.DrawFocusRectangle();
		}

		private void HandleListViewDrawSubItem(object sender, DrawListViewSubItemEventArgs e)
		{
			if (!_enabled)
			{
				e.DrawDefault = true;
				return;
			}

			var flags = GetTextFormatFlags(e.Header.TextAlign);
			var text = e.ItemIndex == -1 ? e.Item.Text : e.SubItem.Text;
			var font = e.ItemIndex == -1 ? e.Item.Font : e.SubItem.Font;
			var color = e.ItemIndex == -1 ? e.Item.ForeColor : e.SubItem.ForeColor;
			var textBounds = new Rectangle(e.Bounds.Location, e.Bounds.Size);

			const int iconSize = 16;
			if (e.ColumnIndex == 0)
			{
				e.Item.ImageList.Draw(e.Graphics, e.Bounds.X + 4, e.Bounds.Y + 1, iconSize, iconSize,
					e.Item.ImageIndex);

				textBounds.Inflate(-iconSize - 4 - 2, 0);
			}

			TextRenderer.DrawText(e.Graphics, " " + text + " ", font, textBounds, color, flags);

			using (var pen = new Pen(Colors.ColumnBorder))
				e.Graphics.DrawLine(pen, e.Bounds.Right - 2, e.Bounds.Y, e.Bounds.Right - 2, e.Bounds.Bottom);
		}

		private void HandleListViewDrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
		{
			if (!_enabled)
			{
				e.DrawDefault = true;
				return;
			}

			var listView = (ListView) sender;
			listView.BackgroundImage = listView.Items.Count == 0 ? _controlStyle.ListViewBackground : null;
			
			var graphics = e.Graphics;
			var r = e.Bounds;

			using (Brush backBrush = new SolidBrush(Colors.HeaderBackground))
			{
				graphics.FillRectangle(backBrush, r);
			}

			using (var pen = new Pen(Colors.LightBorder))
			{
				graphics.DrawLine(pen, r.X, r.Y, r.Right, r.Y);
				graphics.DrawLine(pen, r.Right - 2, r.Y, r.Right - 2, r.Bottom);
			}

			var flags = GetTextFormatFlags(e.Header.TextAlign);
			TextRenderer.DrawText(graphics, " " + e.Header.Text + " ", e.Font, r, Colors.WindowText, flags);
		}

		private static TextFormatFlags GetTextFormatFlags(HorizontalAlignment textAlign)
		{
			var flags = textAlign == HorizontalAlignment.Left
				? TextFormatFlags.Left
				: textAlign == HorizontalAlignment.Center
					? TextFormatFlags.HorizontalCenter
					: TextFormatFlags.Right;

			return flags | TextFormatFlags.WordEllipsis | TextFormatFlags.VerticalCenter;
		}
	}
}