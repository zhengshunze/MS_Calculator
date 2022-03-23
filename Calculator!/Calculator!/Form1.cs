using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Speech.Synthesis;
using System.Threading;
using System.Diagnostics;

namespace Calculator_
{
    public partial class Form1 : Form
    {
        // 使不產生控件閃爍黑洞的問題
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }
        //隱藏游標顯示
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool HideCaret(IntPtr hWnd);

        //文字轉語音
        [DllImport("winmm.dll", EntryPoint = "sndPlaySoundA")]
        public static extern long sndPlaySound(String SoundName, int Flags);

        // 變數宣告
        int curr_x, curr_y;
        bool isWndMove;
        bool isNewEntry;
        bool isRepeatLastOperation;
        bool isInfinityException;
        double dblResult = 0, dblOperand = 0;
        char chPreviousOperator = new char();

        //初始化
        public Form1()
        {
            InitializeComponent();
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();

        }

        //系統載入時
        private void Form1_Load(object sender, EventArgs e)
        {




            timer1.Start();

            textBox1.GotFocus += new EventHandler(textBox1_GotFocus);
            btneq.GotFocus += new EventHandler(btneq_GotFocus);
            new Thread(() =>
            {
                var pb = new PromptBuilder();
                var voice = new System.Speech.Synthesis.SpeechSynthesizer();
                pb.StartVoice("Microsoft Yating Desktop");
                pb.AppendText("歡迎使用小算盤!");
                pb.EndVoice();
                voice.Volume = 100;
                voice.Speak(pb);
            }).Start();

        }

        //去焦點"等於"、"texBox1"
        private void btneq_GotFocus(object sender, EventArgs e)
        {
            //HideCaret(btneq.Handle);
        }
        private void textBox1_GotFocus(object sender, EventArgs e)
        {
            HideCaret(textBox1.Handle);
        }

        //懸停在btn1(視窗上方的"X"、"—"、"口")
        private void btn1_MouseHover(object sender, EventArgs e)
        {
            btn1.FlatAppearance.MouseDownBackColor = Color.FromArgb(50, 255, 0, 0);
            btn1.FlatAppearance.MouseOverBackColor = Color.FromArgb(200, 255, 0, 0);
        }
        private void btn2_MouseHover(object sender, EventArgs e)
        {
            btn2.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 40, 60, 82);
            btn2.FlatAppearance.MouseDownBackColor = Color.FromArgb(40, 40, 60, 82);
        }
        private void btn3_MouseHover(object sender, EventArgs e)
        {
            btn3.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 40, 60, 82);
            btn3.FlatAppearance.MouseDownBackColor = Color.FromArgb(40, 40, 60, 82);
        }

        //視窗(最小化、關閉、開新視窗)
        private void btn3_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            this.Invalidate();
        }
        private void btn1_Click(object sender, EventArgs e)
        {
            DialogResult Result = MessageBox.Show("本程式為參考 Windows 10 內建小算盤的程式，無做商業性之用途!\n若喜歡此程式，可給予開發者大大地的支持，謝謝!\n作者: ZEZE ", "訊息視窗", MessageBoxButtons.OK);


            if (Result == DialogResult.OK)
            {
                Process.Start("https://www.facebook.com/54shzheng");
            }
            timer2.Start();


        }
        private void btn2_Click(object sender, EventArgs e)
        {

            //  new Form2().Show();
        }

        //拖動pnl1
        private void pnl1_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.isWndMove)
            {
                this.Location = new Point(this.Left + e.X - this.curr_x, this.Top + e.Y - this.curr_y);
            }
        }
        private void pnl1_MouseUp(object sender, MouseEventArgs e)
        {
            this.isWndMove = false;
        }
        private void pnl1_MouseDown(object sender, MouseEventArgs e)
        {

            if (e.Button == MouseButtons.Left)
            {
                this.curr_x = e.X;
                this.curr_y = e.Y;
                this.isWndMove = true;
            }
        }

        //滑鼠移動到數字按鈕上
        private void btnf_MouseHover(object sender, EventArgs e)
        {
            //誤刪(滑鼠移動到數字按鈕上的效果)
        }

        //按 "+"、"-"、"*"、"/"
        private void operator_Click(object sender, EventArgs e)
        {
            if (!isInfinityException)
            {
                if (chPreviousOperator == '\0') //選擇任一運算子
                {

                    chPreviousOperator = ((Button)sender).Text[0];
                    dblResult = double.Parse(textBox1.Text);

                    // MessageBox.Show("目前的狀態為:你已按了第一次的運算符號");
                }
                else if (isNewEntry)  //切換任一運算子

                {
                    chPreviousOperator = ((Button)sender).Text[0];

                    //MessageBox.Show("目前的狀態為:你已按了第二次的運算符號");
                }

                else  //進行連續的運算
                {
                    Operate(dblResult, chPreviousOperator, double.Parse(textBox1.Text));
                    chPreviousOperator = ((Button)sender).Text[0];

                    //  MessageBox.Show("目前的狀態為:你正進行連續的運算");

                }

                showdata.Text = textBox1.Text + " " + chPreviousOperator;

                isNewEntry = true; // 會刪除第一次輸入的數字(使textbox1的文字為打入後新輸入的數字)

                isRepeatLastOperation = false; //使計算可處理第二次輸入的數字


            }
        }

        //定義運算子
        private void Operate(double dblPreviousResult, char chPreviousOperator, double dblOperand)
        {
            showdata.Text = dblPreviousResult + " " + chPreviousOperator + " " + dblOperand + " =";

            switch (chPreviousOperator)
            {
                case '+':
                    textBox1.Text = (dblResult = (dblPreviousResult + dblOperand)).ToString();
                    showdata.Text = dblPreviousResult + " " + chPreviousOperator + " " + dblOperand + " =";
                    break;
                case '-':
                    textBox1.Text = (dblResult = (dblPreviousResult - dblOperand)).ToString();
                    showdata.Text = dblPreviousResult + " " + chPreviousOperator + " " + dblOperand + " =";
                    break;
                case 'x':
                    textBox1.Text = (dblResult = (dblPreviousResult * dblOperand)).ToString();
                    showdata.Text = dblPreviousResult + " " + chPreviousOperator + " " + dblOperand + " =";
                    break;
                case '÷':
                    if (!(dblPreviousResult == 0) && dblOperand == 0)
                    {
                        textBox1.Text = "無法除以零";
                        isInfinityException = true;
                        btnadd.Enabled = false;
                        btnmi.Enabled = false;
                        btnx.Enabled = false;
                        btndiv.Enabled = false;
                        btndot.Enabled = false;
                        btnsymbol1.Enabled = false;
                        btnsymbol2.Enabled = false;
                        btnsymbol3.Enabled = false;
                        btnsymbol4.Enabled = false;
                        btnsymbol5.Enabled = false;
                    }
                    else if (dblPreviousResult == 0 && dblOperand == 0)
                    {
                        textBox1.Text = "未定義結果";
                        isInfinityException = true;
                        btnadd.Enabled = false;
                        btnmi.Enabled = false;
                        btnx.Enabled = false;
                        btndiv.Enabled = false;
                        btndot.Enabled = false;
                        btnsymbol1.Enabled = false;
                        btnsymbol2.Enabled = false;
                        btnsymbol3.Enabled = false;
                        btnsymbol4.Enabled = false;
                        btnsymbol5.Enabled = false;
                    }
                    else
                    {
                        textBox1.Text = (dblResult = (dblPreviousResult / dblOperand)).ToString();
                    }
                    break;

                default:
                    break;
            }

        }

        //定義數字鍵
        private void btn_Click(object sender, EventArgs e)
        {

            if (!isInfinityException) //在第一次使用時不發生錯誤時
            {

                if (isNewEntry)  //輸入新數字後會
                {

                    textBox1.Text = "0"; // 將目前textbox1上的數字不進行更新(消除舊數字使數字連續)
                    isNewEntry = false; //  會刪除之前的數字(使textbox1的文字為舊輸入的數字加上打入後輸入的數字)


                    //   MessageBox.Show("目前的狀態為:輸入新數字，會將目前textbox1上的數字不進行更新(消除舊數字使數字連續)，並不能進行先前所輸入的數字(只會進行後續更改的數字運算)");
                }

                if (isRepeatLastOperation) //繼續運算時會
                {
                    chPreviousOperator = '\0'; //可選擇四類的運算子(選擇新運算子將之前的清除)
                    dblResult = 0;            //上個運算的結果清除
                                              //
                                              //  MessageBox.Show("目前的狀態為:單獨運算事件重複做二次以上");
                }

                if (!(textBox1.Text == "0" && (Button)sender == btnf0) && !(((Button)sender) == btndot && textBox1.Text.Contains(".")))
                {
                    textBox1.Text = (textBox1.Text == "0" && ((Button)sender) == btndot) ? "0." : ((textBox1.Text == "0") ? ((Button)sender).Text : textBox1.Text + ((Button)sender).Text);

                }


                isInfinityException = false;
            }
        }

        //定義等於鍵
        private void btneq_Click(object sender, EventArgs e)
        {


            if (!isInfinityException)
            {
                try
                {
                    if (!isRepeatLastOperation)
                    {

                        dblOperand = double.Parse(textBox1.Text);

                        isRepeatLastOperation = true;

                        showdata.Text = "";
                    }

                }

                catch
                {

                }

                Operate(dblResult, chPreviousOperator, dblOperand);
                isNewEntry = true;

            }

            Thread thread = new Thread(new ThreadStart(ansthread));

            thread.Start();


            btnf0.BackColor = Color.White;
            btnf1.BackColor = Color.White;
            btnf2.BackColor = Color.White;
            btnf3.BackColor = Color.White;
            btnf4.BackColor = Color.White;
            btnf5.BackColor = Color.White;
            btnf6.BackColor = Color.White;
            btnf7.BackColor = Color.White;
            btnf8.BackColor = Color.White;
            btnf9.BackColor = Color.White;

        }

        private void ansthread()
        {
            var pb = new PromptBuilder();
            var voice = new System.Speech.Synthesis.SpeechSynthesizer();
            pb.StartVoice("Microsoft Yating Desktop");
            pb.AppendText("等於");
            pb.AppendText(textBox1.Text);
            pb.EndVoice();
            voice.Volume = 100;
            voice.Speak(pb);
        }

        //定義誤打時刪除鍵
        private void btnce_Click(object sender, EventArgs e)
        {

            textBox1.Text = "0";
            this.btnf1.BackColor = Color.White;
            this.btnf2.BackColor = Color.White;
            this.btnf3.BackColor = Color.White;
            this.btnf4.BackColor = Color.White;
            this.btnf5.BackColor = Color.White;
            this.btnf6.BackColor = Color.White;
            this.btnf7.BackColor = Color.White;
            this.btnf8.BackColor = Color.White;
            this.btnf9.BackColor = Color.White;
            this.btndot.BackColor = Color.White;

        }

        //定義完全刪除鍵
        private void btnc_Click(object sender, EventArgs e)
        {
            var pb = new PromptBuilder();
            var voice = new System.Speech.Synthesis.SpeechSynthesizer();
            new Thread(() =>
            {
                pb.StartVoice("Microsoft Yating Desktop");
                pb.AppendText("歸零");
                pb.EndVoice();
                voice.Volume = 100;
                voice.Speak(pb);
            }).Start();
            dblOperand = dblResult = 0; textBox1.Text = "0";
            isInfinityException = false;
            showdata.Text = "";
            chPreviousOperator = '\0';

            btnadd.Enabled = true;
            btnmi.Enabled = true;
            btnx.Enabled = true;
            btndiv.Enabled = true;
            btndot.Enabled = true;
            btnsymbol1.Enabled = true;
            btnsymbol2.Enabled = true;
            btnsymbol3.Enabled = true;
            btnsymbol4.Enabled = true;
            btnsymbol5.Enabled = true;
            this.btnf1.BackColor = Color.White;
            this.btnf2.BackColor = Color.White;
            this.btnf3.BackColor = Color.White;
            this.btnf4.BackColor = Color.White;
            this.btnf5.BackColor = Color.White;
            this.btnf6.BackColor = Color.White;
            this.btnf7.BackColor = Color.White;
            this.btnf8.BackColor = Color.White;
            this.btnf9.BackColor = Color.White;
            this.btndot.BackColor = Color.White;

        }

        //定義退格鍵
        private void buttonback_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Length > 0)
            {
                textBox1.Text = textBox1.Text.Remove(textBox1.Text.Length - 1, 1);
            }
            if (textBox1.Text == "")
            { textBox1.Text = "0"; }

        }

        //觸動鍵盤上的(+ 、 - 、 * 、 / 、 . )
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            var pb = new PromptBuilder();
            var voice = new System.Speech.Synthesis.SpeechSynthesizer();

            switch (e.KeyCode)
            {
                case Keys.Add:
                    this.btnadd.PerformClick();
                    Thread addThread = new Thread(() =>
                    {

                        pb.StartVoice("Microsoft Yating Desktop");
                        pb.AppendText("加");
                        pb.EndVoice();
                        voice.Volume = 100;
                        voice.Speak(pb);


                    });
                    addThread.Start();
                    // sndPlaySound("C:/+.wav", 1);
                    break;
                case Keys.Subtract:
                    this.btnmi.PerformClick();
                    Thread subthread = new Thread(() =>
                    {

                        pb.StartVoice("Microsoft Yating Desktop");
                        pb.AppendText("減");
                        pb.EndVoice();
                        voice.Volume = 100;
                        voice.Speak(pb);
                    });
                    subthread.Start();
                    // sndPlaySound("C:/-.wav", 1);
                    break;
                case Keys.Multiply:
                    this.btnx.PerformClick();
                    Thread multhread = new Thread(() =>
                    {
                        pb.StartVoice("Microsoft Yating Desktop");
                        pb.AppendText("乘");
                        pb.EndVoice();
                        voice.Volume = 100;
                        voice.Speak(pb);
                    });
                    multhread.Start();
                    //sndPlaySound("C:/X.wav", 1);
                    break;
                case Keys.Divide:
                    this.btndiv.PerformClick();
                    Thread divthread = new Thread(() =>
                    {
                        pb.StartVoice("Microsoft Yating Desktop");
                        pb.AppendText("除");
                        pb.EndVoice();
                        voice.Volume = 100;
                        voice.Speak(pb);
                    });
                    divthread.Start();
                    // sndPlaySound("C:/DIV.wav", 1);
                    break;
                case Keys.Decimal:
                    this.btndot.PerformClick();
                    Thread dotthread = new Thread(() =>
                    {
                        pb.StartVoice("Microsoft Yating Desktop");
                        pb.AppendText("點");
                        pb.EndVoice();
                        voice.Volume = 100;
                        voice.Speak(pb);
                    });
                    dotthread.Start();
                    //sndPlaySound("C:/DOT.wav", 1);
                    break;
            }
        }

        //觸動文字貼上ctrl+c
        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.Control | Keys.V))
            {
                try
                {

                    double.Parse((Clipboard.GetText()));
                    textBox1.Text = Clipboard.GetText();

                }

                catch
                {
                    textBox1.Text = "無效的輸入";
                    isInfinityException = true;
                    isNewEntry = false;
                    btnadd.Enabled = false;
                    btnmi.Enabled = false;
                    btnx.Enabled = false;
                    btndiv.Enabled = false;
                    btndot.Enabled = false;
                    btnsymbol1.Enabled = false;
                    btnsymbol2.Enabled = false;
                    btnsymbol3.Enabled = false;
                    btnsymbol4.Enabled = false;
                    btnsymbol5.Enabled = false;

                }

            }
        }

        //以千位數呈現
        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

            if (!string.IsNullOrEmpty(textBox1.Text))
            {

                try
                {
                    System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("en-US");
                    int valueBefore = Int32.Parse(textBox1.Text, System.Globalization.NumberStyles.AllowThousands);
                    textBox1.Text = String.Format(culture, "{0:N0}", valueBefore);
                    textBox1.Select(textBox1.Text.Length, 0);
                }

                catch
                {
                    //donotthing!
                }
            }

        }

        //平方鍵
        private void btnsymbol3_Click(object sender, EventArgs e)
        {
            dblOperand = double.Parse(textBox1.Text);
            textBox1.Text = Math.Pow(dblOperand, 2).ToString();



            var pb = new PromptBuilder();
            var voice = new System.Speech.Synthesis.SpeechSynthesizer();
            pb.StartVoice("Microsoft Yating Desktop");
            pb.AppendText(dblOperand + "的平方等於" + textBox1.Text);

            pb.EndVoice();
            voice.Volume = 100;
            voice.Speak(pb);
            showdata.Text = "sqr" + "(" + dblOperand + ")";
            isNewEntry = true;




        }

        //根號鍵
        private void btnsymbol4_Click(object sender, EventArgs e)
        {
            dblOperand = double.Parse(textBox1.Text);
            textBox1.Text = Math.Pow(dblOperand, 0.5).ToString();
            var pb = new PromptBuilder();
            var voice = new System.Speech.Synthesis.SpeechSynthesizer();
            pb.StartVoice("Microsoft Yating Desktop");
            pb.AppendText(dblOperand + "的根號等於" + textBox1.Text);

            pb.EndVoice();
            voice.Volume = 100;
            voice.Speak(pb);

            showdata.Text = "√" + "(" + dblOperand + ")";
            isNewEntry = true;
        }

        //倒數鍵
        private void btnsymbol2_Click(object sender, EventArgs e)
        {
            dblOperand = double.Parse(textBox1.Text);
            textBox1.Text = Math.Pow(dblOperand, -1).ToString();


            if (dblOperand == 0)
            {
                textBox1.Text = ("無法除以零");
                var pb = new PromptBuilder();
                var voice = new System.Speech.Synthesis.SpeechSynthesizer();
                pb.StartVoice("Microsoft Yating Desktop");
                pb.AppendText("等於無法除以零");
                pb.EndVoice();
                voice.Volume = 100;
                voice.Speak(pb);

            }
            else
            {
                var pb = new PromptBuilder();
                var voice = new System.Speech.Synthesis.SpeechSynthesizer();
                pb.StartVoice("Microsoft Yating Desktop");
                pb.AppendText(dblOperand + "的導數等於" + textBox1.Text);

                pb.EndVoice();
                voice.Volume = 100;
                voice.Speak(pb);
            }
            showdata.Text = "1" + "/" + "(" + dblOperand + ")";
            isNewEntry = true;
        }

        //百分比鍵
        private void btnsymbol5_Click(object sender, EventArgs e)
        {
            MessageBox.Show(" 還沒想到怎麼寫 QQ ");
        }

        private void btnsymbol1_Click(object sender, EventArgs e)
        {
            if (!isInfinityException)
                textBox1.Text = (double.Parse(textBox1.Text) * -1).ToString();
        }

        private void pnl1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Opacity == 1)
            {
                timer1.Stop();
            }
            Opacity += .1;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (Opacity <= 0)
            {
                this.Close();
            }
            Opacity -= .2;
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {

        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            this.Opacity = 0.1;
            timer1.Start();
        }

        private void Form1_Activated(object sender, EventArgs e)
        {

        }

        //MC、MR....那一排的鍵
        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(" 沒用過這些功能(((ﾟДﾟ;))) ");
        }

        //觸動鍵盤上的數字鍵
        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {

            var pb = new PromptBuilder();
            var voice = new System.Speech.Synthesis.SpeechSynthesizer();

            switch (e.KeyChar)
            {
                case (char)48:
                    btnf0.PerformClick();

                    new Thread(() =>
                   {
                       pb.StartVoice("Microsoft Yating Desktop");
                       pb.AppendText("0");
                       pb.EndVoice();
                       voice.Volume = 100;
                       voice.Speak(pb);
                   }).Start();

                    this.btnf0.BackColor = Color.FromArgb(180, 158, 158, 158);
                    this.btnf1.BackColor = Color.White;
                    this.btnf2.BackColor = Color.White;
                    this.btnf3.BackColor = Color.White;
                    this.btnf4.BackColor = Color.White;
                    this.btnf5.BackColor = Color.White;
                    this.btnf6.BackColor = Color.White;
                    this.btnf7.BackColor = Color.White;
                    this.btnf8.BackColor = Color.White;
                    this.btnf9.BackColor = Color.White;
                    break;

                case (char)49:
                    btnf1.PerformClick();
                    new Thread(() =>
                    {
                        pb.StartVoice("Microsoft Yating Desktop");
                        pb.AppendText("1");
                        pb.EndVoice();
                        voice.Volume = 100;
                        voice.Speak(pb);
                    }).Start();
                    this.btnf1.BackColor = Color.FromArgb(180, 158, 158, 158);
                    this.btnf0.BackColor = Color.White;
                    this.btnf2.BackColor = Color.White;
                    this.btnf3.BackColor = Color.White;
                    this.btnf4.BackColor = Color.White;
                    this.btnf5.BackColor = Color.White;
                    this.btnf6.BackColor = Color.White;
                    this.btnf7.BackColor = Color.White;
                    this.btnf8.BackColor = Color.White;
                    this.btnf9.BackColor = Color.White;
                    break;

                case (char)50:
                    btnf2.PerformClick();
                    new Thread(() =>
                    {
                        pb.StartVoice("Microsoft Yating Desktop");
                        pb.AppendText("2");
                        pb.EndVoice();
                        voice.Volume = 100;
                        voice.Speak(pb);
                    }).Start();
                    this.btnf2.BackColor = Color.FromArgb(180, 158, 158, 158);
                    this.btnf0.BackColor = Color.White;
                    this.btnf1.BackColor = Color.White;
                    this.btnf3.BackColor = Color.White;
                    this.btnf4.BackColor = Color.White;
                    this.btnf5.BackColor = Color.White;
                    this.btnf6.BackColor = Color.White;
                    this.btnf7.BackColor = Color.White;
                    this.btnf8.BackColor = Color.White;
                    this.btnf9.BackColor = Color.White;
                    break;

                case (char)51:
                    btnf3.PerformClick();
                    new Thread(() =>
                    {
                        pb.StartVoice("Microsoft Yating Desktop");
                        pb.AppendText("3");
                        pb.EndVoice();
                        voice.Volume = 100;
                        voice.Speak(pb);
                    }).Start();
                    this.btnf3.BackColor = Color.FromArgb(180, 158, 158, 158);
                    btnf0.BackColor = Color.White;
                    btnf1.BackColor = Color.White;
                    btnf2.BackColor = Color.White;
                    btnf4.BackColor = Color.White;
                    btnf5.BackColor = Color.White;
                    btnf6.BackColor = Color.White;
                    btnf7.BackColor = Color.White;
                    btnf8.BackColor = Color.White;
                    btnf9.BackColor = Color.White;
                    break;

                case (char)52:
                    btnf4.PerformClick();
                    new Thread(() =>
                    {
                        pb.StartVoice("Microsoft Yating Desktop");
                        pb.AppendText("4");
                        pb.EndVoice();
                        voice.Volume = 100;
                        voice.Speak(pb);
                    }).Start();
                    this.btnf4.BackColor = Color.FromArgb(180, 158, 158, 158);
                    btnf0.BackColor = Color.White;
                    btnf1.BackColor = Color.White;
                    btnf2.BackColor = Color.White;
                    btnf3.BackColor = Color.White;
                    btnf5.BackColor = Color.White;
                    btnf6.BackColor = Color.White;
                    btnf7.BackColor = Color.White;
                    btnf8.BackColor = Color.White;
                    btnf9.BackColor = Color.White;
                    break;

                case (char)53:
                    btnf5.PerformClick();
                    new Thread(() =>
                    {
                        pb.StartVoice("Microsoft Yating Desktop");
                        pb.AppendText("5");
                        pb.EndVoice();
                        voice.Volume = 100;
                        voice.Speak(pb);
                    }).Start();
                    this.btnf5.BackColor = Color.FromArgb(180, 158, 158, 158);
                    btnf0.BackColor = Color.White;
                    btnf1.BackColor = Color.White;
                    btnf2.BackColor = Color.White;
                    btnf3.BackColor = Color.White;
                    btnf4.BackColor = Color.White;
                    btnf6.BackColor = Color.White;
                    btnf7.BackColor = Color.White;
                    btnf8.BackColor = Color.White;
                    btnf9.BackColor = Color.White;
                    break;

                case (char)54:
                    btnf6.PerformClick();
                    new Thread(() =>
                    {
                        pb.StartVoice("Microsoft Yating Desktop");
                        pb.AppendText("6");
                        pb.EndVoice();
                        voice.Volume = 100;
                        voice.Speak(pb);
                    }).Start();
                    this.btnf6.BackColor = Color.FromArgb(180, 158, 158, 158);
                    btnf0.BackColor = Color.White;
                    btnf1.BackColor = Color.White;
                    btnf2.BackColor = Color.White;
                    btnf3.BackColor = Color.White;
                    btnf4.BackColor = Color.White;
                    btnf5.BackColor = Color.White;
                    btnf7.BackColor = Color.White;
                    btnf8.BackColor = Color.White;
                    btnf9.BackColor = Color.White;
                    break;

                case (char)55:
                    btnf7.PerformClick();
                    new Thread(() =>
                    {
                        pb.StartVoice("Microsoft Yating Desktop");
                        pb.AppendText("7");
                        pb.EndVoice();
                        voice.Volume = 100;
                        voice.Speak(pb);
                    }).Start();
                    this.btnf7.BackColor = Color.FromArgb(180, 158, 158, 158);
                    btnf0.BackColor = Color.White;
                    btnf1.BackColor = Color.White;
                    btnf2.BackColor = Color.White;
                    btnf3.BackColor = Color.White;
                    btnf4.BackColor = Color.White;
                    btnf5.BackColor = Color.White;
                    btnf6.BackColor = Color.White;
                    btnf8.BackColor = Color.White;
                    btnf9.BackColor = Color.White;
                    break;

                case (char)56:
                    btnf8.PerformClick();
                    new Thread(() =>
                    {
                        pb.StartVoice("Microsoft Yating Desktop");
                        pb.AppendText("8");
                        pb.EndVoice();
                        voice.Volume = 100;
                        voice.Speak(pb);
                    }).Start();
                    this.btnf8.BackColor = Color.FromArgb(180, 158, 158, 158);
                    btnf0.BackColor = Color.White;
                    btnf1.BackColor = Color.White;
                    btnf2.BackColor = Color.White;
                    btnf3.BackColor = Color.White;
                    btnf4.BackColor = Color.White;
                    btnf5.BackColor = Color.White;
                    btnf6.BackColor = Color.White;
                    btnf7.BackColor = Color.White;
                    btnf9.BackColor = Color.White;
                    break;

                case (char)57:
                    btnf9.PerformClick();
                    new Thread(() =>
                    {
                        pb.StartVoice("Microsoft Yating Desktop");
                        pb.AppendText("9");
                        pb.EndVoice();
                        voice.Volume = 100;
                        voice.Speak(pb);
                    }).Start();
                    this.btnf9.BackColor = Color.FromArgb(180, 158, 158, 158);
                    btnf0.BackColor = Color.White;
                    btnf1.BackColor = Color.White;
                    btnf2.BackColor = Color.White;
                    btnf3.BackColor = Color.White;
                    btnf4.BackColor = Color.White;
                    btnf5.BackColor = Color.White;
                    btnf6.BackColor = Color.White;
                    btnf7.BackColor = Color.White;
                    btnf8.BackColor = Color.White;
                    break;

                case (char)8:
                    buttonback.PerformClick();
                    break;
                case (char)107:
                    btnadd.PerformClick();

                    break;
                case (char)13:
                    btneq.PerformClick();

                    break;
                case (char)27:
                    btnc.PerformClick();

                    break;
                default:
                    {
                        if (!(e.KeyChar < 57))
                        {
                            e.Handled = true;
                            // textBox1.Text = ("無效的輸入");
                        }

                    }
                    break;
            }

        }


    }
}
