using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Notepad
{
    enum Key { Enter=10};
    public partial class Form1 : Form
    {
        private List<string> _wordsCsharp ;
        private List<string> _wordsSQl;
        private List<string> _currentLanguage;
        private bool _simpleText = true;
        private bool _isInnerChange = false;
        /// <summary>
        /// Активный текст
        /// </summary>
        private RichTextBox _rtbActive = new RichTextBox();
        /// <summary>
        /// Активная вкладка
        /// </summary>
        private int _currentIndexTab;
        /// <summary>
        /// Настройки каждой вкладки
        /// </summary>
        private List<List<bool>> _textSetings = new List<List<bool>>();
        /// <summary>
        /// Номер вкладки
        /// </summary>
        static int Pages = 0;
        public Form1()
        {
            InitializeComponent();
            InitList();
            styleToolStripMenuItem.Enabled = false;
            CurrentLanguagetoolStripTextBox.Text = "Simple text";
            tabPage1.Tag = Pages;
            rtbText.Tag = Pages;
            _currentIndexTab = (int)rtbText.Tag;
            DefaultTextSeting();
            _rtbActive = rtbText;
            rtbText.AllowDrop = true;
            rtbText.DragEnter += TextBoxDragEnter;
            rtbText.DragEnter += TextBoxDragDrop;
        }
        /// <summary>
        /// Инициализация списка ключевых слов
        /// </summary>
        private void InitList()
        {
            //списки не полные !!!
            _wordsCsharp = new List<string>
            {
                "bite","break","in","int","float","double","decimal","using","do","while","string",
                "static","true","false","private","class","public","void","for","foreach","if","else","bool","var","//"
                // символы //-должны быть последними
            };
            _wordsSQl = new List<string>
            {
                "select","update","delete","insert","from","where","alter","table","exec","proc","int","char","group by","into",
                "create","go","database","varchar","references","primary","key","values","use","declare","drop","if","else","begin","end","--",
            };
            _currentLanguage = new List<string>();
        }
        //проверка текста при вводе
        private void rtbText_TextChanged(object sender, EventArgs e)//
        {
            Counter();
            //проверка установлен ли простой текст и делаются ли внутренние изменнения
            if (simpleTextToolStripMenuItem.Checked || _isInnerChange == true||_simpleText) 
                return;

            bool isComent = false;
            Color curColor = _rtbActive.ForeColor;
            int pos = _rtbActive.SelectionStart;
            int line = _rtbActive.GetLineFromCharIndex(_rtbActive.SelectionStart);
            int indexFirstChar = _rtbActive.GetFirstCharIndexFromLine(line);
            int lastIndex = FindLastIndexLine(indexFirstChar,line);
            //выделяем текст и обнуляем цвет текста
            _rtbActive.Select(indexFirstChar, lastIndex - indexFirstChar+1);
            _rtbActive.SelectionColor = curColor;
            //извлекаем строку для проверки на вхождения
            string text= _rtbActive.SelectedText;
            string pattern = "";
            foreach (string word in _currentLanguage)
            {
                if (word == @"//"||word==@"--")
                    pattern = word;
                else
                    pattern = $@"\b{word}\b";
                MatchCollection mc = Regex.Matches(text, pattern);
                
                foreach (Match x in mc)
                {
                    if (word == @"//"||word == @"--")//коментарий окрашиваем в зеленый
                    {
                        _rtbActive.SelectionStart = x.Index + indexFirstChar;
                        _rtbActive.SelectionLength = lastIndex - (x.Index + indexFirstChar-1);
                        isComent = true;
                        _rtbActive.SelectionColor = Color.LimeGreen;
                        break;
                    }
                    _rtbActive.SelectionStart = x.Index + indexFirstChar;
                    _rtbActive.SelectionLength = x.Length;
                    _rtbActive.SelectionColor = Color.Blue;
                }
                if (isComent) break;
            }
            _rtbActive.Select(pos, 0);
            _rtbActive.SelectionColor = curColor;
            Counter();
        }
        /// <summary>
        /// Возвращает последний индекс строки
        /// </summary>
        /// <param name="start"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private int FindLastIndexLine(int start,int line)//
        {
            int index = start;
            while (_rtbActive.GetLineFromCharIndex(index) != (line + 1) && (index + 1) < _rtbActive.Text.Length)
            {
                if ((int)_rtbActive.Text[index] == Key.Enter.GetHashCode())
                    break;
                ++index;
            }
            return index;
        }
        /// <summary>
        /// Открывает файл 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param> 
        private void MenuOpen_Click(object sender, EventArgs e)//
        {
            OpenFileDialog openFile = new OpenFileDialog();
            if (openFile.ShowDialog() != DialogResult.OK)
                return;
            _rtbActive.Clear();
            _isInnerChange = true;
            _rtbActive.Text=(File.ReadAllText(openFile.FileName));
            _rtbActive.SelectionStart = _rtbActive.TextLength;
            Counter();
        }
        /// <summary>
        /// Сохранение файла
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Text files:| *.txt";
            dlg.DefaultExt = "txt";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(dlg.FileName, _rtbActive.Text);
            }
        }
        /// <summary>
        /// Выход из программы с предложением сохраниться
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string msg = "Сохранить файл перед закрытием?";
            DialogResult rezult=MessageBox.Show(msg, "Сообщение", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);
            if(rezult==DialogResult.Yes)
            {
                saveToolStripMenuItem_Click(null, new EventArgs());
            }
            else if(rezult==DialogResult.No)
            {
                Environment.Exit(0);
            }
        }
        /// <summary>
        /// Чтение текста при вводе
        /// </summary>
        private void ReadTextWithSyntax(List<string> words)
        {
            if (_simpleText) return;
            _isInnerChange = true;
            Color curColor = _rtbActive.ForeColor;
            string pattern = "";
            foreach (string word in words)
            {
                if (word == @"//")
                    pattern = word;
                else
                    pattern = $@"\b{word}\b";
                MatchCollection mc = Regex.Matches(_rtbActive.Text, pattern);

                foreach (Match x in mc)
                {
                    if (word == @"//")//коментарий окрашиваем в зеленый
                    {
                        _rtbActive.SelectionStart = x.Index;
                        _rtbActive.SelectionStart = x.Index;
                        int line = _rtbActive.GetLineFromCharIndex(_rtbActive.SelectionStart);
                        int indexFirstChar = _rtbActive.GetFirstCharIndexFromLine(line);
                        int lastIndex = FindLastIndexLine(indexFirstChar, line);
                        int length = lastIndex - x.Index+1;
                        _rtbActive.SelectionLength = length;
                        _rtbActive.SelectionColor = Color.LimeGreen;
                    }
                    else
                    {
                        _rtbActive.SelectionStart = x.Index;
                        _rtbActive.SelectionLength = x.Length;
                        _rtbActive.SelectionColor = Color.Blue;
                    }
                    
                }
            }
            _isInnerChange = false;
            _rtbActive.SelectionStart = _rtbActive.TextLength;
            _rtbActive.SelectionColor = curColor;
            
        }
        /// <summary>
        /// Переключение на обычный текст
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void simpleTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //изменяем состояние чекбоксов
            _simpleText = true;
            simpleTextToolStripMenuItem.Checked = true;
            sharpToolStripMenuItem.Checked = false;
            cplusplusToolStripMenuItem1.Checked = false;
            CurrentLanguagetoolStripTextBox.Text = "Simple text";
            styleToolStripMenuItem.Enabled = false;
            whiteToolStripMenuItem_Click(null, new EventArgs());
            //меняем весь текст на одноцветный
            TextColorClean(Color.Black);
        }
        /// <summary>
        /// Выбирает С++
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cplusplusToolStripMenuItem1_Click(object sender, EventArgs e)//
        {
            _simpleText = false;
            cplusplusToolStripMenuItem1.Checked = true;
            simpleTextToolStripMenuItem.Checked = false;
            sharpToolStripMenuItem.Checked = false;
            CurrentLanguagetoolStripTextBox.Text = "SQL";
            styleToolStripMenuItem.Enabled = true;
            //меняем весь текст на одноцветный
            if (blackToolStripMenuItem.Checked)
                TextColorClean(Color.White);
            else TextColorClean(Color.Black);
            _currentLanguage = _wordsSQl;
            ReadTextWithSyntax(_currentLanguage);
        }
        /// <summary>
        /// Выбирает C#
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sharpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _simpleText = false;
            sharpToolStripMenuItem.Checked = true;
            simpleTextToolStripMenuItem.Checked = false;
            cplusplusToolStripMenuItem1.Checked = false;
            CurrentLanguagetoolStripTextBox.Text = "C#";
            styleToolStripMenuItem.Enabled = true;
            //меняем весь текст на одноцветный
            if(blackToolStripMenuItem.Checked)
                TextColorClean(Color.White);
            else TextColorClean(Color.Black);
            _currentLanguage = _wordsCsharp;
            ReadTextWithSyntax(_currentLanguage);
        }
        /// <summary>
        /// Метод изменяет весь текст на одноцветный
        /// </summary>
        private void TextColorClean(Color color)
        {
            //меняем весь текст на одноцветный
            _rtbActive.SelectAll();
            _isInnerChange = true;
            _rtbActive.SelectionColor = color;
            _rtbActive.ForeColor = color;
            _isInnerChange = false;
            _rtbActive.SelectionStart = _rtbActive.TextLength;
        }
        /// <summary>
        /// Меняет тему на серую
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void blackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            blackToolStripMenuItem.Checked = true;
            whiteToolStripMenuItem.Checked = false;
            _rtbActive.BackColor = SystemColors.InactiveCaptionText;
            TextColorClean(Color.White);
            ReadTextWithSyntax(_currentLanguage);
        }
        /// <summary>
        /// Меняет тему на белую
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void whiteToolStripMenuItem_Click(object sender, EventArgs e)//
        {
            blackToolStripMenuItem.Checked = false;
            whiteToolStripMenuItem.Checked = true;
            _rtbActive.BackColor = SystemColors.Window;
            TextColorClean(Color.Black);
            ReadTextWithSyntax(_currentLanguage);
        }
        /// <summary>
        /// Счетчик линий и символов
        /// </summary>
        private void Counter()
        {
            LengthtoolStripTextBox2.Text = _rtbActive.TextLength.ToString();
            LinestoolStripTextBox3.Text = _rtbActive.Lines.Length.ToString();
            CurLinetoolStripTextBox1.Text = _rtbActive.SelectionStart.ToString();
            CurRowtoolStripTextBox2.Text = (_rtbActive.GetLineFromCharIndex(_rtbActive.SelectionStart)+1).ToString();
        }

        private void rtbText_SelectionChanged(object sender, EventArgs e)
        {
            Counter();
        }

        private void copyToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            rtbText.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int start = _rtbActive.SelectionStart;
            int length = Clipboard.GetText().Length;
            _rtbActive.Text= _rtbActive.Text.Insert(start,Clipboard.GetText());
            _rtbActive.SelectionStart = (start+length);
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            rtbText.Text=rtbText.Text.Remove(rtbText.SelectionStart, rtbText.SelectionLength);
            ReadTextWithSyntax(_currentLanguage);
        }
        /// <summary>
        /// Создание новой вкладки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuCreate_Click(object sender, EventArgs e)
        {
            Pages++;
            int pages=tabControl1.TabCount;
            TabPage page = new TabPage($"New doc({pages})");

            RichTextBox newTextBox = new RichTextBox();
            newTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            newTextBox.AutoWordSelection = true;
            newTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            newTextBox.ContextMenuStrip = this.contextMenuStrip1;
            newTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            newTextBox.ForeColor = System.Drawing.Color.Black;
            newTextBox.Location = new System.Drawing.Point(3, 6);
            newTextBox.Name = "rtbText";
            newTextBox.Size = new System.Drawing.Size(748, 502);
            newTextBox.TabIndex = 1;
            newTextBox.Text = "";
            newTextBox.WordWrap = false;
            newTextBox.SelectionChanged += new System.EventHandler(this.rtbText_SelectionChanged);
            newTextBox.TextChanged += new System.EventHandler(this.rtbText_TextChanged);
            newTextBox.Tag = Pages;
            newTextBox.AllowDrop = true;
            newTextBox.DragEnter += TextBoxDragEnter;
            newTextBox.DragDrop += TextBoxDragDrop;


            page.BackColor = SystemColors.InactiveCaption;
            page.Location = new System.Drawing.Point(4, 22);
            page.Name = "tabPage2";
            page.Size = new System.Drawing.Size(757, 514);
            page.TabIndex = 1;
            page.UseVisualStyleBackColor = true;
            page.Tag = Pages;
            page.Controls.Add(newTextBox);
            tabControl1.TabPages.Add(page);


            DefaultTextSeting();
        }
        /// <summary>
        /// Установка активной вкладки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabControl1_Selected(object sender, TabControlEventArgs e)
        {
            TabControl p = sender as TabControl;
            int index = p.SelectedIndex;
            SaveTextBoxCurSetings(_currentIndexTab);
            foreach (TabPage page in tabControl1.TabPages)
            {
                foreach (var item in page.Controls)
                {
                    if (item is RichTextBox)
                    {
                        RichTextBox rich=item as RichTextBox;
                        if (int.Parse(page.Tag.ToString()) == index)
                        {
                            rich.Enabled = true;
                            _rtbActive = rich;
                            InitCurTextBox(index);
                            _currentIndexTab = (int)rich.Tag;
                        }
                        else rich.Enabled = false;
                        
                    }
                }
            }
        }
        /// <summary>
        /// Инициализирует состояние текущей вкладки
        /// </summary>
        /// <param name="index"></param>
        private void InitCurTextBox(int index)
        {
            if (_textSetings[index][0] == true) simpleTextToolStripMenuItem_Click(null, new EventArgs());
            if (_textSetings[index][1] == true)sharpToolStripMenuItem_Click(null, new EventArgs());
            if (_textSetings[index][2] == true)cplusplusToolStripMenuItem1_Click(null, new EventArgs());
            if (_textSetings[index][3] == true) blackToolStripMenuItem_Click(null, new EventArgs());
            if (_textSetings[index][4] == true) whiteToolStripMenuItem_Click(null, new EventArgs());
        }
        private void DefaultTextSeting()
        {
            //1-Simple text,2-C#,-3C++,4-Black,5-White
            _textSetings.Add(new List<bool> { true, false, false, false, true });
        }
        /// <summary>
        /// Сохраняет текущие настройки для данной вкладки
        /// </summary>
        private void SaveTextBoxCurSetings(int numPage)
        {
            //обнуляем состояние
            for (int i = 0; i < _textSetings[numPage].Count; i++)
            {
                _textSetings[numPage][i] = false;
            }
            //выставляем значения sharpToolStripMenuItem.Checked
            if (simpleTextToolStripMenuItem.Checked == true) _textSetings[numPage][0] = true;
            if (sharpToolStripMenuItem.Checked == true) _textSetings[numPage][1] = true;
            if (cplusplusToolStripMenuItem1.Checked == true) _textSetings[numPage][2] = true;
            if (blackToolStripMenuItem.Checked == true) _textSetings[numPage][3] = true;
            if (whiteToolStripMenuItem.Checked == true) _textSetings[numPage][4] = true;
        }
        private void TextBoxDragEnter(object sender,DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.All;
            }
        }
        private void TextBoxDragDrop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            _rtbActive.Text = File.ReadAllText(files[0],Encoding.Default);
        }

        private void changeTextColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ColorDialog dlg = new ColorDialog();
            if(dlg.ShowDialog()==DialogResult.OK)
            {
                simpleTextToolStripMenuItem_Click(null, new EventArgs());
                TextColorClean(dlg.Color);
                _rtbActive.ForeColor = dlg.Color;
            }
        }

        private void fontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FontDialog dlg = new FontDialog();
            if(dlg.ShowDialog()==DialogResult.OK)
            {
                _rtbActive.Font = dlg.Font;
            }
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _rtbActive.SelectAll();
        }
    }
}




