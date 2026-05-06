using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Liang.LanguageLibrary;
using MSPrinterTools.Properties;

namespace MSPrinterTools;

public class FrmMain : Form
{
	[Flags]
	public enum PortType
	{
		write = 1,
		read = 2,
		redirected = 4,
		net_attached = 8
	}

	public struct PORT_INFO_2
	{
		public string pPortName;

		public string pMonitorName;

		public string pDescription;

		public PortType fPortType;

		internal int Reserved;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void CallbackDelegate(int param1, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] param2, int param3);

	private delegate void delegateUpdateRecvText(int param1, byte[] param2, int param3);

	private SQLiteConnection m_cnn = null;

	private SQLiteCommand m_cmd;

	private SQLiteDataAdapter m_sda_Comm = null;

	private string m_str_Sql;

	private DataSet m_dsRS = null;

	private DataSet m_dsRS_DC = null;

	private DataView m_dv;

	private DataSet m_dsNV = null;

	private int m_iValue1;

	private string m_strValue1;

	private StringBuilder m_sbData;

	private int m_iInit = -1;

	private static int m_iStatus = -1;

	private static int m_iSpecialStatus = -1;

	private bool m_bFirstTime = false;

	private byte[] m_bReceived = new byte[1024];

	private string[,] m_strCodePage_2017ALL = new string[71, 2]
	{
		{ "0", "Chinese" },
		{ "0", "Japanese" },
		{ "0", "Korean" },
		{ "0", "PC437(std.Europe)" },
		{ "1", "Katakana" },
		{ "2", "PC850(Multilingual)" },
		{ "3", "PC860(Portugal)" },
		{ "4", "PC863(Canadian)" },
		{ "5", "PC865(Nordic)" },
		{ "6", "West Europe" },
		{ "7", "Greek" },
		{ "8", "Hebrew" },
		{ "9", "East Europe" },
		{ "10", "Iran" },
		{ "16", "WPC1252" },
		{ "17", "PC866(Cyrillic2)" },
		{ "18", "PC852(Latin2)" },
		{ "19", "PC858" },
		{ "20", "Iran2" },
		{ "21", "Latvian" },
		{ "22", "Arabic" },
		{ "23", "PT1511251" },
		{ "24", "PC747" },
		{ "25", "WPC1257" },
		{ "27", "Vietnam" },
		{ "28", "PC864" },
		{ "29", "PC1001" },
		{ "30", "Uigur" },
		{ "31", "Hebrew" },
		{ "32", "WPC1255(Israel)" },
		{ "255", "Thai" },
		{ "50", "PC437(std.Europe)" },
		{ "51", "Katakana" },
		{ "52", "PC437(std.Europe)" },
		{ "53", "PC858(Multilingual)" },
		{ "54", "PC852(Latin-2)" },
		{ "55", "PC860(Portuguese)" },
		{ "56", "PC861(Icelandic)" },
		{ "57", "PC863(Canadian)" },
		{ "58", "PC865(Nordic)" },
		{ "59", "PC866(Russian)" },
		{ "60", "PC855(Bulgarian)" },
		{ "61", "PC857(Turkey)" },
		{ "62", "PC862(Hebrew)" },
		{ "63", "PC864(Arabic)" },
		{ "64", "PC737(Greek)" },
		{ "65", "PC851(Greek)" },
		{ "66", "PC869(Greek)" },
		{ "67", "PC928(Greek)" },
		{ "68", "PC772(Lithuanian)" },
		{ "69", "PC774(Lithuanian)" },
		{ "70", "PC874(Thai)" },
		{ "71", "WPC1252(Latin-1)" },
		{ "72", "WPC1250(Latin-2)" },
		{ "73", "WPC1251(Cyrillic)" },
		{ "74", "PC3840(IBM-Russian)" },
		{ "75", "PC3841(Gost)" },
		{ "76", "PC3843(Polish)" },
		{ "77", "PC3844(CS2)" },
		{ "78", "PC3845(Hungarian)" },
		{ "79", "PC3846(Turkish)" },
		{ "80", "PC3847(Brazil-ABNT)" },
		{ "81", "PC3848(Brazil-ABICOMP)" },
		{ "82", "PC1001(Arabic)" },
		{ "83", "PC2001(Lithuanian-KBL)" },
		{ "84", "PC3001(Estonian-1)" },
		{ "85", "PC3002(Estonian-2)" },
		{ "86", "PC3011(Latvian-1" },
		{ "87", "PC3012(Latvian-2)" },
		{ "88", "PC3021(Bulgarian)" },
		{ "89", "PC3041(Maltese)" }
	};

	private string[,] m_strCodePage_Epson = new string[42, 2]
	{
		{ "0", "PC437(USA Std Europe)" },
		{ "1", "Katkana" },
		{ "2", "PC850(Multilingual)" },
		{ "3", "PC860(Portuguese)" },
		{ "4", "PC863(Canadian-French)" },
		{ "5", "Nordic" },
		{ "11", "PC851(Greek)" },
		{ "12", "PC853(Turkish)" },
		{ "13", "PC857(Turkish)" },
		{ "14", "PC737(Greek)" },
		{ "15", "ISO8859-7(Greek)" },
		{ "16", "WPC1252" },
		{ "17", "PC866(Cyrillic #2)" },
		{ "18", "PC852(Latin2)" },
		{ "19", "PC858(Euro)" },
		{ "20", "KU42" },
		{ "21", "TIS11(Thai)" },
		{ "26", "TIS18(Thai)" },
		{ "30", "TCVN-3(Vietnamese)" },
		{ "32", "PC720(Arabic)" },
		{ "33", "WPC775(Baltic Rim)" },
		{ "34", "PC855(Cylillic)" },
		{ "35", "PC861(Icelandic)" },
		{ "36", "PC862(Hebrew)" },
		{ "37", "PC864(Arabic)" },
		{ "38", "PC869(Greek)" },
		{ "39", "ISO8859-2" },
		{ "40", "ISO8859-1" },
		{ "41", "PC1098(Farsi)" },
		{ "42", "724(Lithuanian)" },
		{ "43", "722(Lithuanian)" },
		{ "44", "PC1125(Ukrainian)" },
		{ "45", "WPC1250(Latin 2)" },
		{ "46", "WPC1251(Cyrillic)" },
		{ "47", "WPC1253(Greek)" },
		{ "48", "WPC1254(Turkish)" },
		{ "49", "WPC1255(Hebrew)" },
		{ "50", "WPC1256(Arabic)" },
		{ "51", "WPC1257(Baltic Rim)" },
		{ "52", "WPC1258(Vientamese)" },
		{ "53", "KZ1048(Kazakhstan)" },
		{ "255", "User-defined page" }
	};

	private string[,] m_strCodePage_FontB = new string[10, 2]
	{
		{ "0", "PC437" },
		{ "1", "Katakana" },
		{ "2", "PC850" },
		{ "3", "PC860" },
		{ "4", "PC863" },
		{ "5", "PC865" },
		{ "16", "WPC1252" },
		{ "17", "PC866" },
		{ "18", "PC852" },
		{ "19", "PC858" }
	};

	private int m_iMulti2SelectIndex = -1;

	private int m_iDC01SelectIndex = -1;

	private FrmPrintingBox m_frmPrintingBox = new FrmPrintingBox();

	private FrmSpeedBox m_frmSpeedBox = new FrmSpeedBox();

	private StringBuilder m_sbPortName;

	private int m_iBaudrate;

	private static int m_iDevType = 0;

	private int m_iChkIndex = 0;

	public static FrmMain frmMain;

	public static CallbackDelegate callbackDelegate;

	private IContainer components = null;

	private GroupBox groupBox1;

	private ComboBox cboBandrate;

	private Label lblPrintConnValue1;

	private ComboBox cboPort;

	private Label label2;

	private Button btn_SelfCheck;

	private TabControl tabControl1;

	private TabPage tabPage1;

	private TabPage tabPage2;

	private TabPage tabPage3;

	private GroupBox groupBox5;

	private Label label23;

	private TextBox textBox6;

	private Label label24;

	private Button button22;

	private Button button21;

	private ComboBox comboBox18;

	private Label label21;

	private Label label20;

	private Label label19;

	private Button button33;

	private TextBox textBox13;

	private Label label18;

	private Button button20;

	private ComboBox comboBox7;

	private Label label17;

	private Button btnSetting6;

	private ComboBox cboSetting6;

	private Label label16;

	private Button button18;

	private ComboBox comboBox2;

	private Label label15;

	private Button button31;

	private Button button37;

	private ComboBox comboBox4;

	private ComboBox comboBox3;

	private ComboBox comboBox5;

	private Label label13;

	private Label label12;

	private Label label28;

	private ToolStripMenuItem languaToolStripMenuItem;

	private ToolStripMenuItem ToolStripMenuItem;

	private ToolStripMenuItem englishToolStripMenuItem;

	private MenuStrip menuStrip1;

	private Button button30;

	private GroupBox gb_BasicTest;

	private Button btnExample;

	private ComboBox cboExample;

	private Label label1;

	private Button btnPrint1;

	private ComboBox cboSDKFunction;

	private Label label6;

	private TextBox tb_ProductMessage;

	private Button bt_PrintBMP;

	private Label label45;

	private Button bt_GetProductMessage;

	private Button bt_GetStatus;

	private TextBox tb_Status;

	private TextBox tb_bmpFilePath;

	private Label label5;

	private Label label9;

	private GroupBox groupBox2;

	private GroupBox groupBox3;

	private ComboBox comboBox9;

	private GroupBox groupBox4;

	private GroupBox gb_Receive;

	private Button bt_ClearReceiveContent;

	private TextBox tb_ReceiveContent;

	private GroupBox gb_Send;

	private TextBox tb_SendContentH;

	private CheckBox bCutPaper;

	private Button bt_SendToPrinter;

	private TextBox tb_SendContentT;

	private Button bt_ClearSendContent;

	private RadioButton rdb_SendTypeHEX;

	private RadioButton rdb_SendTypeText;

	private Label label10;

	private Label label14;

	private Label label22;

	private GroupBox groupBox6;

	private Label label25;

	private ComboBox comboBox10;

	private Button button1;

	private Button button4;

	private Button button5;

	private ComboBox comboBox8;

	private Button button2;

	private Button button3;

	private CheckBox chkFontEpson;

	private CheckBox chkFontB;

	private CheckBox chkFont2017All;

	private Button btnFont2017All;

	private CheckBox chkSDKFunAll;

	private GroupBox groupBox7;

	private DataGridView dgvRS;

	private DataGridViewTextBoxColumn F_Cmd_ID;

	private DataGridViewCheckBoxColumn F_Select;

	private DataGridViewCheckBoxColumn F_Hex;

	private DataGridViewTextBoxColumn F_Content;

	private DataGridViewTextBoxColumn F_Comment;

	private DataGridViewTextBoxColumn F_Seq;

	private DataGridViewTextBoxColumn F_Sleep;

	private TabPage tabPage4;

	private GroupBox groupBox8;

	private DataGridView dgvNV;

	private Label label26;

	private ComboBox cboNVIndex;

	private Button btnNVPrint;

	private Button btnSetNV;

	private ComboBox cboMulti1;

	private Button btnMulti1;

	private Label label27;

	private ComboBox cboMulti2;

	private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;

	private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;

	private DataGridViewTextBoxColumn F_FilePath;

	private DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;

	private ComboBox cb_Character;

	private ComboBox cboFontLib;

	private Label label4;

	private TabPage tabPage5;

	private TextBox txtWifiIP;

	private Label label7;

	private Button btnWifiSet1;

	private TextBox txtWifiPort;

	private Label label8;

	private TextBox txtWifiPassword;

	private Label label11;

	private TextBox txtWifiWan;

	private Label label29;

	private Button btnWifiSet2;

	private Button btn_SetInit;

	private ComboBox cboWIFIModel;

	private Label label30;

	private Button btnWifiSet3;

	private Button btnWifiSet5;

	private TextBox txtWifiMQTTPort;

	private Label label33;

	private TextBox txtWifiMQTTIP;

	private Label label34;

	private Button btnWifiSet4;

	private TextBox txtWifiMQTTPassword;

	private Label label31;

	private TextBox txtWifiMQTTClientID;

	private Label label32;

	private TextBox txtWifiMQTTClientName;

	private Label label35;

	private Button btnWifiSet6;

	private TextBox txtWifiSubscribe1;

	private Label label36;

	private TextBox txtWifiPublish1;

	private Label label37;

	private GroupBox groupBox9;

	private GroupBox groupBox10;

	private GroupBox groupBox11;

	private Label label41;

	private Label label40;

	private Label label39;

	private Label label38;

	private TextBox txtWifiDNS;

	private TextBox txtWifiGateway;

	private TextBox txtWifiMask;

	private TextBox txtWifiIPAddr;

	private Button btnWifiDNS;

	private Button btnWifiGateway;

	private Button btnWifiMask;

	private Button btnWifiIPAddr;

	private Button btnWifiRead;

	private Button button7;

	private Button button6;

	private TextBox textBox1;

	private Label label42;

	private Label lblCodePage;

	private ComboBox cboCodePage;

	private GroupBox groupBox12;

	private Button btnFontDownload;

	private Label label43;

	private TextBox txtFontPath;

	private Label label44;

	private Button btSpecialStatus;

	private TextBox tb_SpecialStatus;

	private Label label46;

	private Button btPaperDetection;

	private TextBox tb_PaperDetection;

	private Button btnBlackTest;

	private Label label47;

	private TextBox tb_BlackAD;

	private Label label48;

	private Button btnSetBlackAD;

	private TabPage tabPage6;

	private GroupBox groupBox13;

	private Label label51;

	private Label label50;

	private Button btnSpeedStart;

	private TextBox txtSpeedCount;

	private TextBox txtSpeedData;

	private TextBox txtWifiSubscribe3;

	private Label label54;

	private TextBox txtWifiSubscribe2;

	private Label label53;

	private TextBox txtWifiPublish3;

	private Label label52;

	private TextBox txtWifiPublish2;

	private Label label49;

	private CheckBox chkPrintIndex;

	private TabPage tabPage7;

	private ComboBox cboDC01;

	private Label lblDC01;

	private Button btnDCPrint;

	private GroupBox groupBox14;

	private DataGridView dgvDC01;

	private DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;

	private DataGridViewCheckBoxColumn dataGridViewCheckBoxColumn1;

	private DataGridViewCheckBoxColumn F_DCHex;

	private DataGridViewCheckBoxColumn Color;

	private DataGridViewTextBoxColumn F_DCContent;

	private DataGridViewTextBoxColumn F_DCComment;

	private DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;

	private DataGridViewTextBoxColumn dataGridViewTextBoxColumn8;

	private ComboBox cboPrintConnValue2;

	private Label lblPrintConnValue2;

	[DllImport("winspool.drv", CharSet = CharSet.Ansi, EntryPoint = "EnumPortsA", ExactSpelling = true, SetLastError = true)]
	public static extern int EnumPorts(string pName, int Level, IntPtr lpbPorts, int cbBuf, ref int pcbNeeded, ref int pcReturned);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetInit();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern bool PrinterOnline();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern bool GetComCtsStatus();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetClean();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetUsbportauto();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetClose();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetAlignment(int iAlignment);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetBold(int iBold);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetUnderline(int underline);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetSpacechar(int iSpace);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetSpacechinese(int iChsleftspace, int iChsrightspace);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetLeftmargin(int iLeftspace);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetLinespace(int iLinespace);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetPrintport(StringBuilder strPort, int iBaudrate);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetPrintConn(int iConnWay, StringBuilder strName, StringBuilder strValue);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintString(StringBuilder strData);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintSelfcheck();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int GetStatus();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int GetStatusspecial();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintFeedline(int iLine);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetMarkoffsetcut(int iOffset);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetMarkoffsetprint(int iOffset);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintCutpaper(int iMode);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintMarkposition();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintMarkcutpaper(int iMode);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintMarkpositioncut();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintMarkpositionprint();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintFeedDot(int Lnumber);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetSizetext(int iHeight, int iWidth);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetSizechinese(int iHeight, int iWidth, int iUnderline, int iChinesetype);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetSizechar(int iHeight, int iWidth, int iUnderline, int iAsciitype);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetItalic(int iItalic);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetRotate(int iRotate);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetDirection(int iDirection);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetWhitemodel(int iWhite);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintDiskbmpfile(StringBuilder strData);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintDiskimgfile(StringBuilder strData);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int GetDiskImgBuffer(StringBuilder strData);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetNvbmp(int iNums, StringBuilder strData);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintNvmbp(int iNvindex, int iMode);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintQrcode(StringBuilder strData, int iLmargin, int iMside, int iRound);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintQrcodeII(StringBuilder strData, int iLen, int iMside);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintDM(StringBuilder strData, int iSize);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintRemainQR();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int Print1Dbar(int iWidth, int iHeight, int iHrisize, int iHriseat, int iCodetype, StringBuilder strDataint);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintPdf417(int iDotwidth, int iDotheight, int iDatarows, int iDatacolumns, StringBuilder strData);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetCodepage(int country, int CPnumber);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int GetProductinformation(int Fstype, StringBuilder FIDdata, int iFidlen);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int GetSDKinformation(StringBuilder FIDdata);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintTransmit(string strCmd, int iLength);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintTransmit(byte[] strCmd, int iLength);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int GetTransmit(byte[] strCmd, int iLength, byte[] strRecv, int iRelen);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int ReadData(byte[] strRecv, int iRelen);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintString(StringBuilder strData, int iImme);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetCommandmode(int iMode);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintChargeRow();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetAlignmentLeftRight(int iAlignment);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetHTseat(byte[] bHTseat, int iLength);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintNextHT();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintDataMatrix(StringBuilder strData, int iSize);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetPagemode(int iMode, int Xrange, int Yrange);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetPagestartposition(int Xdot, int Ydot);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetPagedirection(int iDirection);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintPagedata();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetRotation_Intomode();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintRotation_Sendtext(StringBuilder strData, int iImme);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintRotation_Sendcode(int leftspace, int iWidth, int iHeight, int iCodetype, StringBuilder strData);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintRotation_Changeline();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int PrintRotation_Data();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetRotation_Leftspace(int iLeftspace);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetComportauto();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int GetTaskStatus();

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetPrintportFlowCtrl(int iFlowCtrlFlag);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int GetBMPBufferDATAExt(StringBuilder strPath, byte[] cImageBuffer);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int GetBMPBufferDCDATA(StringBuilder strPath1, byte[] cImageBuffer, int iBuffLen);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetReadZKmode(int mode);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int SetPrintIDorName(StringBuilder strIDorNAME);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern int GetPrintIDorName(StringBuilder strIDorNAME);

	[DllImport("Msprintsdk.dll", CharSet = CharSet.Ansi)]
	public static extern void CallBackData(CallbackDelegate call);

	private string GetStringRes(string strID, string strDefault)
	{
		return Utility.GetStringRes(strID, strDefault, user1.m_strLanguage);
	}

	public FrmMain()
	{
		InitializeComponent();
		Control.CheckForIllegalCrossThreadCalls = false;
		Text = "PrinterTools V" + Application.ProductVersion.ToString();
	}

	private void controlCharacterEnable()
	{
		groupBox5.Enabled = true;
		groupBox1.Enabled = true;
		languaToolStripMenuItem.Enabled = true;
	}

	private void controlCharacterDisable()
	{
		groupBox5.Enabled = false;
		groupBox1.Enabled = false;
		languaToolStripMenuItem.Enabled = false;
	}

	private void FrmMain_Load(object sender, EventArgs e)
	{
		try
		{
			frmMain = this;
			FunFillPorts();
			cboFont2017_Load();
			cb_Character_Load();
			cboFontB_Load();
			cboFontEpson_Load();
			cboSDKFunction_Load();
			cboExample_Case_Load();
			TabWifi_Load();
			cboSetting6.Items.Add("80mm");
			cboSetting6.Items.Add("72mm");
			cboSetting6.Items.Add("56mm");
			cboSetting6.Items.Add("48mm");
			cboSetting6.SelectedIndex = 1;
			comboBox5.SelectedIndex = 0;
			comboBox4.SelectedIndex = 0;
			comboBox2.SelectedIndex = 0;
			comboBox7.SelectedIndex = 0;
			comboBox5.SelectedIndex = 0;
			comboBox3.SelectedIndex = 0;
			comboBox18.SelectedIndex = 0;
			cb_Character.SelectedIndex = 0;
			int num = 0;
			int num2 = 0;
			string text = "";
			try
			{
				num = Settings.Default.PortTab;
				num2 = Settings.Default.Bandrate;
				m_bFirstTime = Settings.Default.FirstTime;
				text = Settings.Default.PrintConnValue2;
				if (text != "")
				{
					text = "127.0.0.1";
				}
				cboPrintConnValue2.Items.Add(text);
				if (text != "127.0.0.1")
				{
					cboPrintConnValue2.Items.Add("127.0.0.1");
				}
				cboPrintConnValue2.Text = text;
				user1.m_strLanguage = Settings.Default.language;
			}
			catch (Exception)
			{
				Settings.Default.Save();
			}
			if (num == -1)
			{
				num = cboPort.Items.Count - 1;
			}
			if (cboPort.Items.Count > num)
			{
				cboPort.SelectedIndex = num;
			}
			else
			{
				cboPort.SelectedIndex = cboPort.Items.Count - 1;
			}
			if (cboBandrate.Items.Count > num2)
			{
				cboBandrate.SelectedIndex = num2;
			}
			if (user1.m_strLanguage.Equals(""))
			{
				if (SetLanguage.IsChineseSimple())
				{
					user1.m_strLanguage = GlobalVar.g_str_Language_zh_CHS;
					m_bFirstTime = false;
				}
				else
				{
					user1.m_strLanguage = GlobalVar.g_str_Language_en_US;
					m_bFirstTime = false;
				}
				Settings.Default.language = user1.m_strLanguage;
				Settings.Default.FirstTime = m_bFirstTime;
				Settings.Default.Save();
			}
			if (user1.m_strLanguage == GlobalVar.g_str_Language_zh_CHS)
			{
				englishToolStripMenuItem.Checked = false;
				ToolStripMenuItem.Checked = true;
			}
			else
			{
				englishToolStripMenuItem.Checked = true;
				ToolStripMenuItem.Checked = false;
			}
			SetLanguage.SetLang(user1.m_strLanguage, this, typeof(FrmMain));
			CtrlSendContentVisible();
			cboCodePage_Load();
			callbackDelegate = CallbackFunc;
			CallBackData(callbackDelegate);
			GC.KeepAlive(callbackDelegate);
		}
		catch (Exception ex2)
		{
			MessageBox.Show(ex2.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	public static void CallbackFunc(int param1, byte[] param2, int param3)
	{
		try
		{
			if (frmMain.IsHandleCreated)
			{
				if (param3 == 10 || param1 > 1024)
				{
					frmMain.FunClosePort();
					return;
				}
				frmMain.tb_ReceiveContent.BeginInvoke(new delegateUpdateRecvText(UpdateRecvText), param1, param2, param3);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	public static void UpdateRecvText(int param1, byte[] param2, int param3)
	{
		string text = "";
		try
		{
			for (int i = 0; i < param1; i++)
			{
				text = $"{text}{param2[i]:X02} ";
			}
			text = $"Data:{text} ({Encoding.ASCII.GetString(param2)})\r\n";
			frmMain.tb_ReceiveContent.Text += text;
			frmMain.tb_ReceiveContent.Select(frmMain.tb_ReceiveContent.TextLength, 0);
			frmMain.tb_ReceiveContent.ScrollToCaret();
		}
		catch (Exception ex)
		{
			TextBox textBox = frmMain.tb_ReceiveContent;
			textBox.Text = textBox.Text + text + "\r\n" + ex.Message;
		}
	}

	private void cboSDKFunction_Load()
	{
		try
		{
			cboSDKFunction.Items.Add("PrintString");
			cboSDKFunction.Items.Add("PrintChargeRow");
			cboSDKFunction.Items.Add("PrintFeedDot");
			cboSDKFunction.Items.Add("SetLinespace");
			cboSDKFunction.Items.Add("SetSpacechar");
			cboSDKFunction.Items.Add("SetLeftmargin");
			cboSDKFunction.Items.Add("SetSizechar");
			cboSDKFunction.Items.Add("SetSizetext");
			cboSDKFunction.Items.Add("SetAlignment");
			cboSDKFunction.Items.Add("SetAlignmentLeftRight");
			cboSDKFunction.Items.Add("SetBold");
			cboSDKFunction.Items.Add("SetRotate");
			cboSDKFunction.Items.Add("SetDirection");
			cboSDKFunction.Items.Add("SetWhitemodel");
			cboSDKFunction.Items.Add("SetItalic");
			cboSDKFunction.Items.Add("SetUnderline");
			cboSDKFunction.Items.Add("SetHTseat");
			cboSDKFunction.Items.Add("PrintQrcode");
			cboSDKFunction.Items.Add("PrintDataMatrix");
			cboSDKFunction.Items.Add("SetCodepage");
			cboSDKFunction.Items.Add("Print1Dbar");
			cboSDKFunction.SelectedIndex = 0;
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void cboExample_Case_Load()
	{
		try
		{
			string[,] array = new string[2, 2]
			{
				{ "1", "Example01(58mm)" },
				{ "2", "Example02" }
			};
			DataTable dataTable = new DataTable();
			DataColumn dataColumn = new DataColumn();
			dataColumn.DataType = Type.GetType("System.Int32");
			dataColumn.ColumnName = "id";
			dataTable.Columns.Add(dataColumn);
			dataColumn = new DataColumn();
			dataColumn.DataType = Type.GetType("System.String");
			dataColumn.ColumnName = "ExampleCase";
			dataTable.Columns.Add(dataColumn);
			int num = array.GetUpperBound(0) + 1;
			int num2 = 0;
			for (num2 = 0; num2 < num; num2++)
			{
				DataRow dataRow = dataTable.NewRow();
				dataRow["id"] = array[num2, 0];
				dataRow["ExampleCase"] = array[num2, 1];
				dataTable.Rows.Add(dataRow);
			}
			cboExample.DataSource = dataTable;
			cboExample.DisplayMember = "ExampleCase";
			cboExample.ValueMember = "id";
			cboExample.SelectedIndex = 0;
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void FunFillPorts()
	{
		try
		{
			cboBandrate.Items.Add("115200");
			cboBandrate.Items.Add("57600");
			cboBandrate.Items.Add("38400");
			cboBandrate.Items.Add("19200");
			cboBandrate.Items.Add("9600");
			cboBandrate.SelectedIndex = 0;
			cboPort.Items.Clear();
			cboPort.Items.Add("USBAuto");
			int num = 0;
			int pcbNeeded = 0;
			int pcReturned = 0;
			IntPtr zero = IntPtr.Zero;
			IntPtr zero2 = IntPtr.Zero;
			PORT_INFO_2[] array = null;
			num = EnumPorts("", 2, zero, 0, ref pcbNeeded, ref pcReturned);
			zero = Marshal.AllocHGlobal(Convert.ToInt32(pcbNeeded + 1));
			if (EnumPorts("", 2, zero, pcbNeeded, ref pcbNeeded, ref pcReturned) != 0)
			{
				zero2 = zero;
				array = new PORT_INFO_2[pcReturned];
				for (int i = 0; i < pcReturned; i++)
				{
					ref PORT_INFO_2 reference = ref array[i];
					reference = (PORT_INFO_2)Marshal.PtrToStructure(zero2, typeof(PORT_INFO_2));
					if (array[i].pPortName.StartsWith("USB"))
					{
						cboPort.Items.Add(array[i].pPortName);
					}
					zero2 = (IntPtr)(zero2.ToInt32() + Marshal.SizeOf(typeof(PORT_INFO_2)));
				}
				zero2 = IntPtr.Zero;
			}
			if (zero != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(zero);
				zero = IntPtr.Zero;
				zero2 = IntPtr.Zero;
			}
			string[] portNames = SerialPort.GetPortNames();
			cboPort.Items.Insert(cboPort.Items.Count, "COMAuto");
			foreach (string text in portNames)
			{
				string text2 = text.Substring(3);
				string text3 = "COM";
				int num2 = 0;
				for (num2 = 0; num2 < text2.Length && Regex.IsMatch(text2.Substring(num2, 1), "^[+-]?\\d*$"); num2++)
				{
					text3 += text2.Substring(num2, 1);
				}
				if (int.Parse(text3.Substring(3)) > 30)
				{
					text3 = text3.Substring(0, 4);
				}
				if (text3 != "")
				{
					cboPort.Items.Add(text3);
				}
			}
			cboPort.Items.Add("LPT1");
			cboPort.Items.Add("BT MS-BL58");
			cboPort.Items.Add("BT MSP-100");
			cboPort.Items.Add("BT MS-MD80I-BT");
			cboPort.Items.Add("Network");
			cboPort.SelectedIndex = 0;
			Win32Utility.SetCueText(tb_bmpFilePath, "Double click to select image file...");
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "FillPorts", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void btn_SelfCheck_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) == 0 && PrintSelfcheck() != 0)
			{
				MessageBox.Show(GetStringRes("R10023", "Failure!"), "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private int FunOpenPort(int iMsgBox)
	{
		int num = 1;
		if (m_iInit == 0)
		{
			return 0;
		}
		Cursor = Cursors.WaitCursor;
		try
		{
			m_sbPortName = new StringBuilder(cboPort.Text, cboPort.Text.Length);
			if (m_sbPortName.ToString() == "USBAuto")
			{
				SetUsbportauto();
				m_iDevType = 2;
			}
			else if (m_sbPortName.ToString() == "COMAuto")
			{
				SetComportauto();
				m_iDevType = 1;
			}
			else if (m_sbPortName.ToString() == "Network")
			{
				m_iDevType = 6;
				SetPrintConn(m_iDevType, new StringBuilder(cboPrintConnValue2.Text), new StringBuilder(""));
			}
			else
			{
				m_iBaudrate = int.Parse(cboBandrate.Text);
				if (m_sbPortName.ToString().StartsWith("USB"))
				{
					m_iDevType = 2;
					SetPrintConn(m_iDevType, m_sbPortName, new StringBuilder(""));
				}
				else if (m_sbPortName.ToString().StartsWith("COM"))
				{
					m_iDevType = 5;
					StringBuilder strValue = new StringBuilder(m_iBaudrate.ToString());
					SetPrintConn(m_iDevType, m_sbPortName, strValue);
				}
				else if (m_sbPortName.ToString().StartsWith("LPT"))
				{
					m_iDevType = 4;
					SetPrintConn(m_iDevType, m_sbPortName, new StringBuilder(""));
				}
				else if (m_sbPortName.ToString().StartsWith("BT "))
				{
					m_iDevType = 3;
					StringBuilder strName = new StringBuilder(m_sbPortName.ToString().Substring(3));
					SetPrintConn(m_iDevType, strName, new StringBuilder("1234"));
				}
			}
			num = SetInit();
			if (num == 0)
			{
				m_iInit = 0;
				btn_SetInit.Text = GetStringRes("R10025", "Close");
				cboPort.Enabled = false;
				cboBandrate.Enabled = false;
				Settings.Default.PortTab = cboPort.SelectedIndex;
				Settings.Default.Bandrate = cboBandrate.SelectedIndex;
				Settings.Default.PrintConnValue2 = cboPrintConnValue2.Text;
				Settings.Default.Save();
			}
			else if (iMsgBox == 1)
			{
				MessageBox.Show(GetStringRes("R10001", "Open port failure!"), "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		Cursor = Cursors.Default;
		return num;
	}

	private int FunClosePort()
	{
		int result = SetClose();
		m_iInit = -1;
		btn_SetInit.Text = GetStringRes("R10024", "Open");
		cboPort.Enabled = true;
		cboBandrate.Enabled = true;
		return result;
	}

	private int FunOpenFailure()
	{
		MessageBox.Show(GetStringRes("R10001", "Open port failure!"), "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		return 0;
	}

	private void btnSetting6_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) == 0)
			{
				m_iValue1 = cboSetting6.SelectedIndex;
				switch (m_iValue1)
				{
				case 0:
					m_strValue1 = "\u0013tDw\0";
					break;
				case 1:
					m_strValue1 = "\u0013tDw\u0011";
					break;
				case 2:
					m_strValue1 = "\u0013tDw\"";
					break;
				case 3:
					m_strValue1 = "\u0013tDw3";
					break;
				default:
					m_strValue1 = "\u0013tDw\u0011";
					break;
				}
				PrintTransmit(m_strValue1, m_strValue1.Length);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void btnPrint1_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) == 0)
			{
				int i = 0;
				int num = cboSDKFunction.Items.Count;
				string text = "";
				if (!chkSDKFunAll.Checked)
				{
					i = cboSDKFunction.SelectedIndex;
					num = i + 1;
				}
				for (; i < num; i++)
				{
					SetClean();
					Thread.Sleep(30);
					SetCommandmode(3);
					text = cboSDKFunction.GetItemText(cboSDKFunction.Items[i]) + "Demo";
					PrintFeedDot(10);
					m_sbData = new StringBuilder(text + ";");
					PrintString(m_sbData, 0);
					Type type = Type.GetType("MSPrinterTools.FrmMain");
					MethodInfo method = type.GetMethod(text);
					object obj = Activator.CreateInstance(type);
					method.Invoke(obj, null);
					PrintFeedDot(20);
					Thread.Sleep(600);
				}
				PrintFeedDot(240);
				PrintCutpaper(1);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	public void PrintStringDemo()
	{
		m_sbData = new StringBuilder("PrintString(sbData,1)");
		PrintString(m_sbData, 1);
		m_sbData = new StringBuilder("PrintString(sbData,1)");
		PrintString(m_sbData, 1);
		m_sbData = new StringBuilder("PrintString(sbData,0)");
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("PrintString(sbData,0)");
		PrintString(m_sbData, 0);
	}

	public void SetSizecharDemo()
	{
		m_sbData = new StringBuilder("SetSizechar(0,0,0,0)");
		SetSizechar(0, 0, 0, 0);
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("SetSizechar(1,1,0,0)");
		SetSizechar(1, 1, 0, 0);
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("SetSizechar(1,1,1,1)");
		SetSizechar(1, 1, 1, 1);
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("SetSizechar(1,0,0,0)");
		SetSizechar(1, 0, 0, 0);
		PrintString(m_sbData, 0);
	}

	public void PrintCutpaperDemo()
	{
		m_sbData = new StringBuilder("PrintCutpaper(0)");
		PrintString(m_sbData, 0);
		PrintFeedDot(250);
		PrintCutpaper(0);
		m_sbData = new StringBuilder("PrintCutpaper(1)");
		PrintString(m_sbData, 0);
		PrintFeedDot(250);
		PrintFeedDot(250);
		PrintCutpaper(1);
	}

	public void PrintChargeRowDemo()
	{
		m_sbData = new StringBuilder("PrintChargeRow()");
		PrintString(m_sbData, 1);
		PrintChargeRow();
	}

	public void PrintFeedDotDemo()
	{
		m_sbData = new StringBuilder("PrintFeedDot(30)");
		PrintFeedDot(30);
	}

	public void SetLinespaceDemo()
	{
		m_sbData = new StringBuilder("SetLinespace(30)");
		SetLinespace(30);
		PrintString(m_sbData, 0);
		PrintString(m_sbData, 0);
		PrintString(m_sbData, 0);
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("SetLinespace(45)");
		SetLinespace(45);
		PrintString(m_sbData, 0);
		PrintString(m_sbData, 0);
		PrintString(m_sbData, 0);
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("SetLinespace(60)");
		SetLinespace(60);
		PrintString(m_sbData, 0);
		PrintString(m_sbData, 0);
		PrintString(m_sbData, 0);
		PrintString(m_sbData, 0);
	}

	public void SetSpacecharDemo()
	{
		m_sbData = new StringBuilder("SetSpacechar(5)");
		SetSpacechar(5);
		PrintString(m_sbData, 0);
		PrintString(m_sbData, 0);
		PrintString(m_sbData, 0);
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("SetSpacechar(10)");
		SetSpacechar(10);
		PrintString(m_sbData, 0);
		PrintString(m_sbData, 0);
		PrintString(m_sbData, 0);
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("SetSpacechar(15)");
		SetSpacechar(15);
		PrintString(m_sbData, 0);
		PrintString(m_sbData, 0);
		PrintString(m_sbData, 0);
		PrintString(m_sbData, 0);
	}

	public void SetLeftmarginDemo()
	{
		m_sbData = new StringBuilder("SetLeftmargin(5)");
		SetLeftmargin(10);
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("SetLeftmargin(10)");
		SetLeftmargin(20);
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("SetLeftmargin(15)");
		SetLeftmargin(30);
		PrintString(m_sbData, 0);
	}

	public void SetSizetextDemo()
	{
		m_sbData = new StringBuilder("SetSizetext(1,1)");
		SetSizetext(1, 1);
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("SetSizetext(2,2)");
		SetSizetext(2, 2);
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("SetSizetext(3,3)");
		SetSizetext(3, 3);
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("SetSizetext(4,3)");
		SetSizetext(4, 3);
		PrintString(m_sbData, 0);
	}

	public void SetAlignmentDemo()
	{
		m_sbData = new StringBuilder("SetAlignment(0)");
		SetAlignment(0);
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("SetAlignment(1)");
		SetAlignment(1);
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("SetAlignment(2)");
		SetAlignment(2);
		PrintString(m_sbData, 0);
	}

	public void SetAlignmentLeftRightDemo()
	{
		m_sbData = new StringBuilder("SetAlignmentLeft");
		SetAlignmentLeftRight(0);
		PrintString(m_sbData, 1);
		m_sbData = new StringBuilder("SetAlignmentRight");
		SetAlignmentLeftRight(2);
		PrintString(m_sbData, 0);
	}

	public void SetBoldDemo()
	{
		m_sbData = new StringBuilder("SetBold(1):1");
		SetBold(1);
		PrintString(m_sbData, 1);
		m_sbData = new StringBuilder("SetBold(0):1");
		SetBold(0);
		PrintString(m_sbData, 1);
		m_sbData = new StringBuilder("SetBold(1):2");
		SetBold(1);
		PrintString(m_sbData, 1);
		m_sbData = new StringBuilder("SetBold(0):2");
		SetBold(0);
		PrintString(m_sbData, 0);
	}

	public void SetRotateDemo()
	{
		m_sbData = new StringBuilder("SetRotate(1)");
		SetRotate(1);
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("SetRotate(0)");
		SetRotate(0);
		PrintString(m_sbData, 0);
	}

	public void SetDirectionDemo()
	{
		m_sbData = new StringBuilder("SetDirection(1)");
		SetDirection(1);
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("SetDirection(0)");
		SetDirection(0);
		PrintString(m_sbData, 0);
	}

	public void SetWhitemodelDemo()
	{
		m_sbData = new StringBuilder("SetWhitemodel(1)");
		SetWhitemodel(1);
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("SetWhitemodel(0)");
		SetWhitemodel(0);
		PrintString(m_sbData, 0);
	}

	public void SetItalicDemo()
	{
		m_sbData = new StringBuilder("SetItalic(1)");
		SetItalic(1);
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("SetItalic(0)");
		SetItalic(0);
		PrintString(m_sbData, 0);
	}

	public void SetUnderlineDemo()
	{
		m_sbData = new StringBuilder("SetUnderline(2)");
		SetUnderline(2);
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("SetUnderline(1)");
		SetUnderline(1);
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("SetUnderline(0)");
		SetUnderline(0);
		PrintString(m_sbData, 0);
	}

	public void SetHTseatDemo()
	{
		m_sbData = new StringBuilder("1");
		SetHTseat(new byte[3] { 10, 18, 25 }, 3);
		PrintString(m_sbData, 1);
		PrintNextHT();
		m_sbData = new StringBuilder("2");
		PrintString(m_sbData, 1);
		PrintNextHT();
		m_sbData = new StringBuilder("3");
		PrintString(m_sbData, 1);
		PrintNextHT();
		m_sbData = new StringBuilder("4");
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("1a");
		PrintString(m_sbData, 1);
		PrintNextHT();
		m_sbData = new StringBuilder("2a");
		PrintString(m_sbData, 1);
		PrintNextHT();
		m_sbData = new StringBuilder("3a");
		PrintString(m_sbData, 1);
		PrintNextHT();
		m_sbData = new StringBuilder("4a");
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("1b");
		PrintString(m_sbData, 1);
		PrintNextHT();
		m_sbData = new StringBuilder("2b");
		PrintString(m_sbData, 1);
		PrintNextHT();
		m_sbData = new StringBuilder("3b");
		PrintString(m_sbData, 1);
		PrintNextHT();
		m_sbData = new StringBuilder("4b");
		PrintString(m_sbData, 0);
	}

	public void PrintQrcodeDemo()
	{
		m_sbData = new StringBuilder("PrintQrcode(strData, 10, 6, 0)");
		PrintString(m_sbData, 0);
		PrintQrcode(m_sbData, 10, 6, 0);
		PrintRemainQR();
		if (m_iDevType == 1 || m_iDevType == 5)
		{
			Thread.Sleep(500);
		}
		PrintFeedDot(60);
		SetLeftmargin(0);
		m_sbData = new StringBuilder("PrintQrcode(strData, 0, 6, 1)");
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("QR Code:123456");
		PrintQrcode(m_sbData, 0, 6, 1);
		SetLeftmargin(140);
		m_sbData = new StringBuilder("QR Code:");
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("123456");
		PrintString(m_sbData, 0);
		PrintRemainQR();
		if (m_iDevType == 1 || m_iDevType == 5)
		{
			Thread.Sleep(500);
		}
		PrintFeedDot(60);
		SetLeftmargin(0);
		m_sbData = new StringBuilder("PrintQrcode(strData, 15, 5, 1)");
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("QR Code:123456");
		PrintQrcode(m_sbData, 15, 6, 1);
		m_sbData = new StringBuilder("QR Code:");
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("123456");
		PrintString(m_sbData, 0);
		PrintRemainQR();
		PrintFeedDot(60);
		Thread.Sleep(240);
	}

	public void Print1DbarDemo()
	{
		m_sbData = new StringBuilder("UPC-A");
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("123456789012");
		Print1Dbar(4, 72, 0, 1, 0, m_sbData);
		PrintFeedDot(30);
		m_sbData = new StringBuilder("UPC-E");
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("012345678912");
		Print1Dbar(3, 72, 0, 1, 1, m_sbData);
		PrintFeedDot(30);
		m_sbData = new StringBuilder("EAN-13");
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("1234567890123");
		Print1Dbar(3, 72, 0, 1, 2, m_sbData);
		PrintFeedDot(30);
		m_sbData = new StringBuilder("EAN-8");
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("12345678");
		Print1Dbar(3, 72, 0, 1, 3, m_sbData);
		PrintFeedDot(30);
		m_sbData = new StringBuilder("CODE39");
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("123456");
		Print1Dbar(2, 72, 0, 1, 4, m_sbData);
		PrintFeedDot(30);
		m_sbData = new StringBuilder("ITF 12060001234");
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("120600010001234");
		Print1Dbar(3, 120, 0, 1, 5, m_sbData);
		PrintFeedDot(30);
		m_sbData = new StringBuilder("CODABAR");
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("A123456B");
		Print1Dbar(4, 72, 0, 1, 6, m_sbData);
		PrintFeedDot(30);
		m_sbData = new StringBuilder("Standard EAN13");
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("123456789012");
		Print1Dbar(2, 72, 0, 1, 7, m_sbData);
		PrintFeedDot(30);
		m_sbData = new StringBuilder("Standard EAN8");
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("123456789012");
		Print1Dbar(4, 72, 0, 1, 8, m_sbData);
		PrintFeedDot(30);
		m_sbData = new StringBuilder("CODE93");
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("123456");
		Print1Dbar(3, 72, 0, 1, 9, m_sbData);
		PrintFeedDot(30);
		m_sbData = new StringBuilder("CODE128");
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("A1234567890");
		Print1Dbar(2, 72, 2, 1, 10, m_sbData);
		PrintFeedDot(30);
		PrintFeedDot(60);
		Thread.Sleep(100);
	}

	public void PrintDataMatrixDemo()
	{
		m_sbData = new StringBuilder("DataMatrix");
		if (m_iDevType == 1 || m_iDevType == 5)
		{
			Thread.Sleep(1500);
		}
		SetLeftmargin(60);
		PrintDataMatrix(m_sbData, 10);
		if (m_iDevType == 1 || m_iDevType == 5)
		{
			Thread.Sleep(1500);
		}
	}

	public void SetCodepageDemo()
	{
		SetCodepage(0, 18);
		byte[] array = new byte[128];
		int iLength = 0;
		array[iLength++] = 159;
		array[iLength++] = 216;
		array[iLength++] = 229;
		array[iLength++] = 253;
		array[iLength++] = 231;
		array[iLength++] = 133;
		array[iLength++] = 236;
		array[iLength++] = 167;
		PrintTransmit(array, iLength);
		PrintChargeRow();
		SetClean();
		SetCommandmode(3);
		SetCodepage(5, 16);
		m_sbData = new StringBuilder("$ € ¢ £ ¥");
		PrintString(m_sbData, 0);
		SetClean();
		SetCommandmode(3);
	}

	public void SetPagemodeDemo()
	{
		SetClean();
		SetCommandmode(3);
		m_sbData = new StringBuilder("SetTop");
		PrintString(m_sbData, 0);
		PrintFeedDot(40);
		SetPagemode(1, 576, 640);
		SetPagestartposition(0, 0);
		SetPagedirection(0);
		m_sbData = new StringBuilder("000000000073751872");
		PrintString(m_sbData, 0);
		PrintFeedDot(20);
		PrintString(m_sbData, 0);
		SetPagedirection(1);
		SetPagestartposition(80, 60);
		m_sbData = new StringBuilder("000000000073751872");
		PrintPagedata();
		SetPagemode(0, 576, 640);
		m_sbData = new StringBuilder("bottom");
		PrintString(m_sbData, 0);
		PrintFeedDot(120);
	}

	public void RotationDemo()
	{
		SetRotation_Intomode();
		m_sbData = new StringBuilder("iWidth = 3");
		PrintRotation_Sendtext(m_sbData, 0);
		m_sbData = new StringBuilder("120600010007409577");
		PrintRotation_Sendcode(0, 3, 3, 5, m_sbData);
		PrintRotation_Changeline();
		PrintRotation_Changeline();
		PrintRotation_Data();
	}

	private void ToolStripMenuItem_Click(object sender, EventArgs e)
	{
		if (!ToolStripMenuItem.Checked)
		{
			ToolStripMenuItem.Checked = true;
			englishToolStripMenuItem.Checked = false;
		}
		user1.m_strLanguage = GlobalVar.g_str_Language_zh_CHS;
		Settings.Default.language = user1.m_strLanguage;
		SetLanguage.SetLang(user1.m_strLanguage, this, typeof(FrmMain));
		Settings.Default.Save();
		CtrlSendContentVisible();
	}

	private void englishToolStripMenuItem_Click(object sender, EventArgs e)
	{
		if (!englishToolStripMenuItem.Checked)
		{
			englishToolStripMenuItem.Checked = true;
			ToolStripMenuItem.Checked = false;
		}
		user1.m_strLanguage = GlobalVar.g_str_Language_en_US;
		Settings.Default.language = user1.m_strLanguage;
		SetLanguage.SetLang(user1.m_strLanguage, this, typeof(FrmMain));
		Settings.Default.Save();
		CtrlSendContentVisible();
	}

	private void button30_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) == 0)
			{
				byte[] array = new byte[5];
				int selectedIndex = comboBox3.SelectedIndex;
				array[0] = 19;
				array[1] = 116;
				array[2] = 17;
				array[3] = 68;
				if (selectedIndex == 0)
				{
					array[4] = 68;
				}
				else
				{
					array[4] = 187;
				}
				PrintTransmit(array, array.Length);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void button31_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) == 0)
			{
				byte[] array = new byte[5];
				int selectedIndex = comboBox4.SelectedIndex;
				array[0] = 19;
				array[1] = 116;
				array[2] = 17;
				array[3] = 85;
				if (selectedIndex == 0)
				{
					array[4] = 85;
				}
				else
				{
					array[4] = 170;
				}
				PrintTransmit(array, array.Length);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void button37_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) == 0)
			{
				int selectedIndex = comboBox5.SelectedIndex;
				byte[] array = new byte[5] { 19, 116, 34, 68, 0 };
				if (selectedIndex == 0)
				{
					array[4] = 68;
				}
				else
				{
					array[4] = 187;
				}
				PrintTransmit(array, array.Length);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void button18_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) == 0)
			{
				int selectedIndex = comboBox2.SelectedIndex;
				byte[] array = new byte[5] { 19, 116, 68, 51, 0 };
				switch (selectedIndex)
				{
				case 0:
					array[4] = 51;
					break;
				case 1:
					array[4] = 53;
					break;
				default:
					array[4] = 204;
					break;
				}
				PrintTransmit(array, array.Length);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void button20_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) == 0)
			{
				int selectedIndex = comboBox7.SelectedIndex;
				byte[] array = new byte[5] { 19, 116, 68, 68, 0 };
				if (selectedIndex == 0)
				{
					array[4] = 68;
				}
				else
				{
					array[4] = 187;
				}
				PrintTransmit(array, array.Length);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void button21_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) == 0)
			{
				byte[] array = new byte[5] { 19, 116, 68, 85, 0 };
				switch (comboBox18.SelectedIndex)
				{
				case 4:
					array[4] = 9;
					break;
				case 3:
					array[4] = 19;
					break;
				case 2:
					array[4] = 3;
					break;
				case 1:
					array[4] = 5;
					break;
				default:
					array[4] = 1;
					break;
				}
				PrintTransmit(array, array.Length);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void button22_Click(object sender, EventArgs e)
	{
		try
		{
			int num = Convert.ToInt32(textBox6.Text.Trim());
			if (num < 70 || num > 200)
			{
				MessageBox.Show("Invalid parameter!", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				textBox6.Focus();
			}
			else if (FunOpenPort(1) == 0)
			{
				byte[] array = new byte[5]
				{
					19,
					116,
					68,
					102,
					(byte)num
				};
				PrintTransmit(array, array.Length);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void button33_Click(object sender, EventArgs e)
	{
		try
		{
			int num = Convert.ToInt32(textBox13.Text.Trim());
			if (num < 0 || num > 1600)
			{
				MessageBox.Show("Invalid parameter!", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				textBox13.Focus();
			}
			else if (FunOpenPort(1) == 0)
			{
				byte[] array = new byte[6]
				{
					19,
					116,
					17,
					120,
					(byte)(num / 256),
					(byte)(num % 256)
				};
				PrintTransmit(array, array.Length);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void bt_GetStatus_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) == 0)
			{
				long num = Environment.TickCount;
				m_iStatus = GetStatus();
				long num2 = Environment.TickCount;
				if (m_iStatus == 1)
				{
					m_iStatus = GetStatus();
				}
				switch (m_iStatus)
				{
				case 0:
					tb_Status.Text = m_iStatus + " - " + GetStringRes("R10014", "Printer is ready");
					break;
				case 1:
					tb_Status.Text = m_iStatus + " - " + GetStringRes("R10015", "Printer is offline or no power");
					break;
				case 2:
					tb_Status.Text = m_iStatus + " - " + GetStringRes("R10016", "Printer called unmatched library");
					break;
				case 3:
					tb_Status.Text = m_iStatus + " - " + GetStringRes("R10017", "Printer head is opened");
					break;
				case 4:
					tb_Status.Text = m_iStatus + " - " + GetStringRes("R10018", "Cutter is not reset");
					break;
				case 5:
					tb_Status.Text = m_iStatus + " - " + GetStringRes("R10019", "Printer head temp is abnormal");
					break;
				case 6:
					tb_Status.Text = m_iStatus + " - " + GetStringRes("R10020", "Printer does not detect blackmark");
					break;
				case 7:
					tb_Status.Text = m_iStatus + " - " + GetStringRes("R10021", "Paper out");
					break;
				case 8:
					tb_Status.Text = m_iStatus + " - " + GetStringRes("R10022", "Paper low");
					break;
				}
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void bt_GetProductMessage_Click(object sender, EventArgs e)
	{
		try
		{
			int i = 0;
			int num = 0;
			byte[] array = new byte[3] { 29, 73, 3 };
			byte[] array2 = new byte[64];
			if (FunOpenPort(1) != 0)
			{
				return;
			}
			tb_ProductMessage.Text = "";
			num = GetTransmit(array, array.Length, array2, array2.Length);
			if (num <= 0)
			{
				MessageBox.Show(GetStringRes("R10004", "Check failure!"), "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			byte[] array3 = new byte[num];
			for (; i < num; i++)
			{
				array3[i] = array2[i];
			}
			string text = Encoding.Default.GetString(array3);
			tb_ProductMessage.Text = text;
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void bt_PrintBMP_Click(object sender, EventArgs e)
	{
		byte[] array = new byte[1000];
		try
		{
			if (FunOpenPort(1) != 0)
			{
				return;
			}
			if (tb_bmpFilePath.Text == "")
			{
				MessageBox.Show(GetStringRes("R10005", "Image path is empty!"), "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			Cursor = Cursors.WaitCursor;
			SetClean();
			Thread.Sleep(10);
			StringBuilder strData = new StringBuilder(tb_bmpFilePath.Text);
			PrintDiskimgfile(strData);
			PrintFeedline(10);
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		Cursor = Cursors.Default;
	}

	private void btnCodepageTest_Click_1(object sender, EventArgs e)
	{
		if (FunOpenPort(1) == 0)
		{
			string text = " !\"#$%&'()*+,-./\n0123456789:;<=>?\n@ABCDEFGHIJKLMNO\nPQRSTUVWXYZ[\\]^_\n`abcdefghijklmno\npqrstuvwxyz{|}~\u007f\n";
			byte[] array = new byte[136]
			{
				128, 129, 130, 131, 132, 133, 134, 135, 136, 137,
				138, 139, 140, 141, 142, 143, 10, 144, 145, 146,
				147, 148, 149, 150, 151, 152, 153, 154, 155, 156,
				157, 158, 159, 10, 160, 161, 162, 163, 164, 165,
				166, 167, 168, 169, 170, 171, 172, 173, 174, 175,
				10, 176, 177, 178, 179, 180, 181, 182, 183, 184,
				185, 186, 187, 188, 189, 190, 191, 10, 192, 193,
				194, 195, 196, 197, 198, 199, 200, 201, 202, 203,
				204, 205, 206, 207, 10, 208, 209, 210, 211, 212,
				213, 214, 215, 216, 217, 218, 219, 220, 221, 222,
				223, 10, 224, 225, 226, 227, 228, 229, 230, 231,
				232, 233, 234, 235, 236, 237, 238, 239, 10, 240,
				241, 242, 243, 244, 245, 246, 247, 248, 249, 250,
				251, 252, 253, 254, 255, 10
			};
			PrintTransmit(text, text.Length);
			PrintTransmit(array, array.Length);
			Thread.Sleep(10);
			PrintFeedline(10);
			PrintCutpaper(0);
		}
	}

	private void cb_Character_Load()
	{
		DataTable dataTable = new DataTable();
		DataColumn dataColumn = new DataColumn();
		dataColumn.DataType = Type.GetType("System.Int32");
		dataColumn.ColumnName = "id";
		dataTable.Columns.Add(dataColumn);
		dataColumn = new DataColumn();
		dataColumn.DataType = Type.GetType("System.String");
		dataColumn.ColumnName = "Character";
		dataTable.Columns.Add(dataColumn);
		int num = m_strCodePage_2017ALL.GetUpperBound(0) + 1;
		int num2 = 0;
		for (num2 = 0; num2 < num; num2++)
		{
			DataRow dataRow = dataTable.NewRow();
			dataRow["id"] = m_strCodePage_2017ALL[num2, 0];
			dataRow["Character"] = m_strCodePage_2017ALL[num2, 1];
			dataTable.Rows.Add(dataRow);
		}
		cb_Character.DataSource = dataTable;
		cb_Character.DisplayMember = "Character";
		cb_Character.ValueMember = "id";
		comboBox8.DataSource = dataTable;
		comboBox8.DisplayMember = "Character";
		comboBox8.ValueMember = "id";
	}

	private void cboFont2017_Load()
	{
		DataTable dataTable = new DataTable();
		DataColumn dataColumn = new DataColumn();
		dataColumn.DataType = Type.GetType("System.Int32");
		dataColumn.ColumnName = "id";
		dataTable.Columns.Add(dataColumn);
		dataColumn = new DataColumn();
		dataColumn.DataType = Type.GetType("System.String");
		dataColumn.ColumnName = "Ver";
		dataTable.Columns.Add(dataColumn);
		DataRow dataRow = dataTable.NewRow();
		dataRow["id"] = 1;
		dataRow["Ver"] = "Epic-Font B";
		dataTable.Rows.Add(dataRow);
		dataRow = dataTable.NewRow();
		dataRow["id"] = 2;
		dataRow["Ver"] = "Epic-Font 2017All";
		dataTable.Rows.Add(dataRow);
		cboFontLib.DataSource = dataTable;
		cboFontLib.DisplayMember = "Ver";
		cboFontLib.ValueMember = "id";
	}

	private void cboFontEpson_Load()
	{
		DataTable dataTable = new DataTable();
		DataColumn dataColumn = new DataColumn();
		dataColumn.DataType = Type.GetType("System.Int32");
		dataColumn.ColumnName = "id";
		dataTable.Columns.Add(dataColumn);
		dataColumn = new DataColumn();
		dataColumn.DataType = Type.GetType("System.String");
		dataColumn.ColumnName = "Character";
		dataTable.Columns.Add(dataColumn);
		int num = m_strCodePage_Epson.GetUpperBound(0) + 1;
		int num2 = 0;
		for (num2 = 0; num2 < num; num2++)
		{
			DataRow dataRow = dataTable.NewRow();
			dataRow["id"] = m_strCodePage_Epson[num2, 0];
			dataRow["Character"] = m_strCodePage_Epson[num2, 1];
			dataTable.Rows.Add(dataRow);
		}
		comboBox10.DataSource = dataTable;
		comboBox10.DisplayMember = "Character";
		comboBox10.ValueMember = "id";
	}

	private void cboFontB_Load()
	{
		DataTable dataTable = new DataTable();
		DataColumn dataColumn = new DataColumn();
		dataColumn.DataType = Type.GetType("System.Int32");
		dataColumn.ColumnName = "id";
		dataTable.Columns.Add(dataColumn);
		dataColumn = new DataColumn();
		dataColumn.DataType = Type.GetType("System.String");
		dataColumn.ColumnName = "Character";
		dataTable.Columns.Add(dataColumn);
		int num = m_strCodePage_FontB.GetUpperBound(0) + 1;
		int num2 = 0;
		for (num2 = 0; num2 < num; num2++)
		{
			DataRow dataRow = dataTable.NewRow();
			dataRow["id"] = m_strCodePage_FontB[num2, 0];
			dataRow["Character"] = m_strCodePage_FontB[num2, 1];
			dataTable.Rows.Add(dataRow);
		}
		comboBox9.DataSource = dataTable;
		comboBox9.DisplayMember = "Character";
		comboBox9.ValueMember = "id";
	}

	private void cboFontLib_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (cboFontLib.SelectedIndex == 0)
		{
			cb_Character.Visible = false;
			comboBox9.Visible = true;
		}
		else if (cboFontLib.SelectedIndex == 1)
		{
			cb_Character.Visible = true;
			comboBox9.Visible = false;
		}
	}

	private void bt_ClearReceiveContent_Click(object sender, EventArgs e)
	{
		tb_ReceiveContent.Clear();
	}

	private void PrintCharacterPage()
	{
		string text = " !\"#$%&'()*+,-./\n0123456789:;<=>?\n@ABCDEFGHIJKLMNO\nPQRSTUVWXYZ[\\]^_\n`abcdefghijklmno\npqrstuvwxyz{|}~\u007f\n";
		byte[] array = new byte[136]
		{
			128, 129, 130, 131, 132, 133, 134, 135, 136, 137,
			138, 139, 140, 141, 142, 143, 10, 144, 145, 146,
			147, 148, 149, 150, 151, 152, 153, 154, 155, 156,
			157, 158, 159, 10, 160, 161, 162, 163, 164, 165,
			166, 167, 168, 169, 170, 171, 172, 173, 174, 175,
			10, 176, 177, 178, 179, 180, 181, 182, 183, 184,
			185, 186, 187, 188, 189, 190, 191, 10, 192, 193,
			194, 195, 196, 197, 198, 199, 200, 201, 202, 203,
			204, 205, 206, 207, 10, 208, 209, 210, 211, 212,
			213, 214, 215, 216, 217, 218, 219, 220, 221, 222,
			223, 10, 224, 225, 226, 227, 228, 229, 230, 231,
			232, 233, 234, 235, 236, 237, 238, 239, 10, 240,
			241, 242, 243, 244, 245, 246, 247, 248, 249, 250,
			251, 252, 253, 254, 255, 10
		};
		PrintTransmit(text, text.Length);
		PrintTransmit(array, array.Length);
		PrintFeedline(10);
		PrintCutpaper(1);
	}

	private void button2_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) != 0)
			{
				return;
			}
			SetCommandmode(3);
			int selectedIndex = cb_Character.SelectedIndex;
			byte[] array;
			if (selectedIndex < 3)
			{
				array = new byte[5] { 19, 116, 51, 85, 0 };
				switch (selectedIndex)
				{
				case 1:
					array[4] = 2;
					break;
				case 2:
					array[4] = 1;
					break;
				}
				PrintTransmit(array, array.Length);
				Thread.Sleep(3000);
				if (FunOpenPort(1) == 0)
				{
					SetCommandmode(3);
					button3_Click(sender, e);
					Thread.Sleep(1000);
					FunClosePort();
				}
				return;
			}
			int num = int.Parse(cb_Character.SelectedValue.ToString());
			array = new byte[5] { 19, 116, 51, 85, 3 };
			PrintTransmit(array, array.Length);
			Thread.Sleep(3000);
			if (FunOpenPort(1) == 0)
			{
				SetCommandmode(3);
				byte[] array2 = new byte[5]
				{
					19,
					116,
					51,
					102,
					(byte)num
				};
				PrintTransmit(array2, array2.Length);
				Thread.Sleep(1200);
				FunClosePort();
				Thread.Sleep(3500);
				button3_Click(sender, e);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void button3_Click(object sender, EventArgs e)
	{
		if (FunOpenPort(1) == 0)
		{
			PrintCharacterPage();
		}
	}

	private void bt_SendToPrinter_Click(object sender, EventArgs e)
	{
		if (FunOpenPort(1) != 0)
		{
			return;
		}
		try
		{
			string text = "";
			text = ((!rdb_SendTypeHEX.Checked) ? tb_SendContentT.Text : tb_SendContentH.Text);
			int num = 0;
			if (text == "")
			{
				MessageBox.Show(GetStringRes("R10002", "The input character is empty!"), "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			else if (rdb_SendTypeHEX.Checked)
			{
				byte[] array;
				try
				{
					array = Utility.StrToHexByte(text);
					num = array.Length;
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
					MessageBox.Show(GetStringRes("R10003", "The input character format is wrong!"), "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return;
				}
				Cursor = Cursors.WaitCursor;
				PrintTransmit(array, num);
				Thread.Sleep(300);
				Cursor = Cursors.Default;
			}
			else if (rdb_SendTypeText.Checked)
			{
				int codepage = int.Parse(cboCodePage.SelectedValue.ToString());
				byte[] bytes = Encoding.GetEncoding(codepage).GetBytes(text);
				PrintTransmit(bytes, bytes.Length);
				PrintFeedline(2);
				if (bCutPaper.Checked)
				{
					PrintFeedline(3);
					PrintCutpaper(0);
				}
			}
		}
		catch (Exception ex)
		{
			Cursor = Cursors.Default;
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void rdb_SendTypeText_CheckedChanged(object sender, EventArgs e)
	{
		CtrlSendContentVisible();
	}

	private void rdb_SendTypeHEX_CheckedChanged(object sender, EventArgs e)
	{
		CtrlSendContentVisible();
	}

	private void CtrlSendContentVisible()
	{
		tb_SendContentH.Visible = rdb_SendTypeHEX.Checked;
		tb_SendContentT.Visible = rdb_SendTypeText.Checked;
		lblCodePage.Visible = rdb_SendTypeText.Checked;
		cboCodePage.Visible = rdb_SendTypeText.Checked;
		if (m_iInit == 0)
		{
			btn_SetInit.Text = GetStringRes("R10025", "Close");
		}
		else
		{
			btn_SetInit.Text = GetStringRes("R10024", "Open");
		}
		lblPrintConnValue1.Text = GetStringRes("R10035", "Baudrate") + ":";
		cboPort_SelectedIndexChanged(null, null);
	}

	private void bt_ClearSendContent_Click(object sender, EventArgs e)
	{
		if (rdb_SendTypeHEX.Checked)
		{
			tb_SendContentH.Clear();
		}
		else
		{
			tb_SendContentT.Clear();
		}
	}

	private void bt_ClearReceiveContent_Click_1(object sender, EventArgs e)
	{
		tb_ReceiveContent.Clear();
	}

	private void btnExample_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) == 0)
			{
				SetClean();
				string name = "ExampleDemo" + cboExample.SelectedValue.ToString();
				Type type = Type.GetType("MSPrinterTools.FrmMain");
				MethodInfo method = type.GetMethod(name);
				object obj = Activator.CreateInstance(type);
				method.Invoke(obj, null);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	public void ExampleDemo1()
	{
		if (File.Exists("test1.bmp"))
		{
			m_sbData = new StringBuilder("test1.bmp");
			PrintDiskimgfile(m_sbData);
		}
		else
		{
			string text = "1D 76 30 00 20 00 7D 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 E0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FC 1F F1 FF 80 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 FE 3F F7 FF 80 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 0F FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 0F FF FF FF FF FF 80 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 3F 3F FF EF F8 3F FF C0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 3F 7F FF CF F8 7F FF C0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 7F F9 FE 1F 87 FE 1F FE 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FF F1 FC 1F 87 F8 3F FF 80 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 FF F1 FC 0F 07 C1 FF FF C0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03 E7 F0 F0 00 01 C3 FF FF C0 00 00 00 00 00 00 00 00 00 00 00 01 C0 00 00 00 00 00 00 00 00 03 C7 E7 E0 00 0E 80 07 F8 3F 80 00 00 00 00 00 00 00 00 00 00 00 03 E0 00 00 00 00 00 00 00 00 07 E7 C7 E0 07 FF F8 03 F0 7F E0 00 00 00 00 00 00 00 00 00 00 00 03 E0 00 00 00 00 00 00 00 00 0F FF C7 87 FF FF FF E0 07 FF FC 00 00 00 00 00 00 00 00 00 00 00 01 F0 00 00 00 00 00 00 00 00 0F FF C7 8F FF FF FF F0 07 FF FC 00 00 00 00 00 00 00 00 00 00 00 01 F0 00 00 00 00 00 00 00 00 0F FF C0 FF FC 00 1F FF 07 E3 F8 00 00 01 FC 00 00 00 00 00 00 00 01 F0 00 00 00 00 00 00 00 00 0F FF C1 FF 80 00 00 FF C1 1F E0 00 00 0F FF 80 00 00 00 00 00 00 01 F0 00 00 00 00 00 00 00 00 0F DF C3 FC 00 00 00 3F F0 3F FE 00 00 1F FF E0 00 00 00 00 00 00 00 F8 00 00 00 00 00 00 00 00 07 CF C7 F0 00 00 00 0F F8 7F FF 00 00 1F 83 F0 00 00 00 00 00 00 00 F8 00 00 00 00 00 00 00 00 01 E7 3F 00 00 01 F8 00 7F 00 3F 00 01 FC 00 3E 00 00 00 00 00 00 00 7C 00 00 00 00 00 00 00 00 01 F0 3F 00 3C 01 F8 00 3F 80 7F 00 03 F8 00 1E 00 00 00 00 00 00 00 7C 00 00 00 00 00 00 00 00 01 F0 3F 00 3E 01 F8 00 1F C3 FF 00 03 F0 00 0E 00 00 00 00 00 00 00 7C 00 00 00 00 00 00 00 00 00 F9 FC 00 1E 00 00 00 07 F3 F8 00 03 E0 00 00 00 00 00 00 60 00 1F FC 00 00 00 00 00 00 00 00 00 7F F8 00 00 00 00 00 01 FC 00 00 03 C0 00 00 00 00 00 01 FE 00 3F FE 00 00 00 00 00 00 00 00 00 7F F0 00 00 00 00 00 01 FC 00 00 03 C0 00 00 00 00 00 03 FE 00 7F FE 00 00 00 00 00 00 00 00 00 07 C0 00 00 00 00 00 00 7E 00 00 0F 80 00 00 00 7F E0 07 FF C0 7C 7F 00 00 00 00 00 00 00 00 00 07 C0 00 00 00 00 00 00 3F 7F 00 0F 80 00 00 00 FF F8 07 9F E1 F8 3F 00 00 00 00 00 00 00 00 00 0F C0 00 00 0E 00 00 00 3F FF 80 0F 00 00 1F E0 FF F8 0F 87 E1 F8 1F 00 00 00 00 00 00 00 00 00 1F 00 00 03 FF C0 00 00 1F FF F0 0F 00 7F FF F0 F0 3E 0E 01 F1 F8 1F 00 03 E0 00 00 00 00 00 00 1F 00 00 03 FF E0 00 00 0F FF F0 0F 00 FF FF F0 F0 1E 1E 01 F3 F0 1F 00 03 E0 00 00 00 00 00 00 3E 00 00 03 F1 F0 00 00 07 C0 F0 0F 00 FF C1 F0 F0 1F 1E 01 FB F0 0F 00 03 E0 00 00 00 00 00 00 78 00 00 0F E1 F8 00 00 07 E0 78 0F 00 60 01 F1 F0 1F 1E 01 F9 F8 0F 00 03 C0 00 00 00 00 00 00 78 00 00 07 E0 F0 00 00 03 E0 F8 0F 00 00 01 F1 F0 1F 3E 01 F9 F8 1F 80 07 C0 00 00 00 00 00 00 78 00 00 07 80 00 00 00 03 E0 F8 0F 00 00 01 F1 F0 1F 1E 01 F9 F8 3F 80 07 C0 00 00 00 00 00 FF F8 00 00 00 00 00 00 00 03 E1 F8 07 C0 00 01 F0 F0 1F 1F 01 F0 7F FF C0 0F C0 00 00 00 00 00 FF F8 00 00 00 00 00 00 00 03 E1 F0 07 C0 00 01 F0 F0 1F 0F 81 F0 7F FF C0 0F 80 00 00 00 00 03 FF F8 00 00 00 00 00 00 00 03 FF F0 03 E0 00 03 F0 FC 1F 07 FF E0 1F FF 80 0F 00 00 00 00 00 07 E1 F8 00 00 00 00 00 00 00 03 FF E0 03 E0 00 03 E0 FC 1F 07 FF E0 07 E0 00 1F 00 00 00 00 00 07 C0 78 00 F8 00 00 00 00 00 03 FF 80 03 F0 00 07 E0 7F FE 01 FF 80 00 00 00 1F 00 00 00 00 00 0F C0 78 00 FC 00 00 00 1E 00 07 FF 00 03 F8 00 0F E0 3F FE 00 7E 00 00 00 00 1F 00 00 00 00 00 07 C0 78 00 7E 00 00 00 3F 00 07 C0 00 00 FF 00 1F 80 07 F8 00 00 00 00 00 00 1C 00 00 00 00 00 07 C0 78 00 7F 00 00 00 3F 00 0F C0 00 00 3F 80 3F 80 03 F0 00 00 00 00 00 00 1C 00 00 00 00 00 07 C0 78 00 1F 80 00 00 3E 00 1F C0 00 00 1F F1 FE 00 00 00 00 01 F0 00 00 00 3C 00 00 00 00 00 07 E0 3C 00 0F C0 00 01 FC 00 3F 80 00 00 07 FF FE 00 00 00 00 01 F0 00 00 00 3C 00 00 00 00 00 03 E0 1E 00 0F F8 00 07 F0 00 3F 00 00 00 00 FF F8 00 00 00 00 01 F8 00 00 00 3C 00 00 00 00 00 03 E0 1F 00 01 FE 00 07 F0 00 3F 00 00 00 00 7F F0 00 00 00 00 01 F8 00 00 00 3C 00 00 00 00 00 00 FF FF C0 00 7F FF FF C0 00 FC 00 00 00 00 00 00 00 00 00 00 00 7C 07 C0 00 78 00 00 00 00 00 00 FF FF C0 00 3F FF FF 80 00 FC 00 00 00 00 00 00 00 00 00 00 00 7C 07 C0 00 78 00 00 00 00 00 00 3F FF C0 00 03 FF F8 00 01 F8 00 00 00 00 00 30 00 00 00 00 00 3E 07 C0 00 78 00 00 00 00 00 00 01 E7 F0 00 00 0F 00 00 07 F0 00 00 00 00 00 7E 00 00 00 00 00 1E 1F 80 00 70 00 00 00 00 00 00 00 03 F8 00 00 00 00 00 0F C0 00 00 00 00 00 7E 00 00 00 00 00 1E 1F 80 00 F0 00 00 00 00 00 00 00 00 FC 00 00 00 00 00 0F C0 00 00 00 00 00 7E 00 00 00 00 00 1E 3E 00 00 F0 00 00 00 00 00 00 00 00 3F C0 00 00 00 00 FE 00 00 00 00 00 00 7E 00 00 00 FF C0 1F FC 00 00 F0 00 00 00 00 00 00 00 00 1F F0 00 00 00 03 FE 00 07 00 00 18 00 7E 00 00 07 FF E0 1F FC 00 01 F0 00 00 00 00 00 00 00 00 0F F0 00 00 00 07 FC 00 0F 00 00 3C 00 7E 00 00 07 FF E0 1F FC 00 01 F0 00 00 00 00 00 00 00 00 00 FF F0 00 07 FF FE 00 07 C0 00 7C 00 7E 00 FC 07 E1 F0 1F FC 00 01 F0 00 00 00 00 00 00 00 00 00 1F FF FF FF FF 3E 00 01 F0 00 3F 00 7E 03 FF C7 C3 F0 0F FF 80 01 E0 00 00 00 00 00 00 00 00 00 1F FF FF FF FE 3E 00 01 F0 00 3F 80 7E 07 FF C7 E3 F0 0F FF 80 01 E0 00 00 00 00 00 00 00 00 D8 1F FF FF FF E0 3E 00 00 F0 00 3F 80 7E 07 FF E7 E3 E0 0F EF C0 01 F0 00 00 00 00 00 00 00 07 02 7E 00 1E 0F C0 0F 00 00 78 00 7F 80 7E 0F 83 E3 E7 E0 07 C3 F0 00 40 00 00 00 00 00 00 00 06 00 FE 00 00 0F C0 0F 00 00 78 00 7F C0 7E 0F 01 E3 FF C0 07 C3 F8 00 00 00 00 00 00 00 00 00 06 44 FE 00 00 0F C0 0F 80 00 3E 00 7F E0 7E 0F 01 F3 FF E0 07 C0 7E 00 00 00 00 00 00 00 00 00 07 08 FC 00 00 07 C0 0F 80 00 0F 00 7C F0 7E 0F 01 F1 FF FC 03 E0 3F 00 00 00 00 00 00 00 00 00 00 32 F8 00 00 07 C0 07 C0 00 0F 80 FC 78 7E 0F 01 F9 FD FE 03 E0 1F 00 00 00 00 00 00 00 00 00 00 82 F0 00 00 07 C0 07 C0 00 0F C0 FC 78 7E 0F 00 F9 FC 7F 83 F0 1F 00 00 00 00 00 00 00 00 00 07 20 F0 00 00 07 C0 03 E0 00 07 E0 FC 7E 7E 0F 81 F8 F8 0F F1 F8 00 0F 80 00 00 00 00 00 00 00 03 A0 F0 00 00 07 C0 03 F0 00 03 F0 FC 7E 7E 0F 81 F8 F8 03 F1 E0 00 0F C0 00 00 00 00 00 00 00 03 61 F0 00 00 07 C0 01 F8 00 00 F0 F8 3F 7E 07 81 F0 FC 01 E0 40 00 0F C0 00 00 00 00 00 00 00 01 01 F0 00 00 03 E0 01 FC 00 00 FC F8 1F FE 07 C3 F0 FC 00 00 00 00 03 80 00 00 00 00 00 00 00 07 81 F0 00 00 01 F0 00 FC 00 00 7C F8 07 FE 03 FF E0 10 00 00 00 00 00 00 00 00 00 00 00 00 00 03 C1 F0 00 00 01 F0 00 FC 00 00 3C F8 07 FE 01 FF E0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 41 F0 00 00 00 F0 00 3E 00 00 1E F8 03 FE 00 7F C0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03 F0 00 00 00 70 00 3F 00 00 0F F8 01 FE 00 1E 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03 E0 00 00 00 78 00 3F 80 00 07 F8 00 F8 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03 E0 00 00 00 3C 00 1F 80 00 03 FC 00 78 00 00 00 00 00 00 7C 00 00 00 00 00 00 00 00 00 00 06 03 E0 00 00 00 3C 00 07 C0 00 03 FC 00 00 00 00 00 00 7F FF FF 00 00 00 00 00 00 00 00 00 00 00 03 E0 00 00 00 3C 00 07 E0 00 03 FC 00 00 00 00 00 07 FF FF FF 00 00 00 00 00 00 00 00 00 00 00 07 E0 00 00 00 3E 00 03 F0 00 03 F0 00 00 00 00 3F FF FF E0 00 00 00 00 00 00 00 00 00 00 00 00 07 E0 00 00 00 3F 00 03 F0 00 01 F0 00 00 00 03 FF FF F8 00 00 00 00 00 00 00 00 00 00 00 00 00 07 E0 00 00 00 3F 00 03 F8 00 00 00 00 00 00 3F FF FC 00 00 00 00 00 00 00 00 00 00 00 00 00 00 07 C0 00 00 00 1F 00 00 FC 00 00 00 00 00 1F FF F8 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 07 C0 00 00 00 0F 80 00 7E 00 00 00 00 07 FF FC 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 07 C0 00 00 00 0F C0 00 7F 00 00 00 00 3F FF C0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 07 C0 00 00 00 07 C0 00 1F 00 00 00 1F FF C0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 07 80 00 00 00 03 E0 00 1F 80 00 00 7F FE 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 07 C0 00 00 00 03 F0 00 0F 80 00 03 FF F0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 07 C0 1F F8 00 01 F0 00 07 C0 00 1F F8 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 07 80 1F FE 00 01 F0 00 07 E0 00 1F E0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 07 C0 3F FF F0 00 F8 00 03 F0 00 1E 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 07 C0 7E 07 FF C0 F8 00 00 F0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 07 C0 FE 01 FF C0 78 00 00 F0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 07 C0 FC 00 7F F0 7C 00 00 F8 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 07 E1 F8 00 1F FF BE 00 00 7C 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 07 E3 F0 00 1F FF FE 00 00 3E 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03 EF E0 00 07 CF FE 00 00 3F 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03 EF C0 00 07 C1 FF 00 00 0F 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03 FF 80 00 07 E0 0F 00 00 0F C0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03 FF 80 00 03 E0 0F 00 00 0F C0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 FF 00 00 00 F0 00 00 00 07 F0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 FE 00 00 00 78 00 07 00 03 F0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 FE 00 00 00 3C 00 3F FE 00 F8 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 FC 00 00 00 3E 00 7F FF FF FC 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 F8 00 00 00 3F 01 FC 3F FF FC 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 F8 00 00 00 3F 01 F8 00 FF FC 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 F0 00 00 00 1F 87 C0 00 00 0C 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 20 00 00 00 0F C7 C0 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 07 EF 80 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03 FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03 FE 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 FE 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FC 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FC 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 F8 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ";
			byte[] array = Utility.StrToHexByte(text + "1D 72 01");
			PrintTransmit(array, array.Length);
		}
		SetReadZKmode(0);
		PrintFeedDot(30);
		byte[] bytes = Encoding.GetEncoding("gb2312").GetBytes("店号：8888          机号：100001\n");
		PrintTransmit(bytes, bytes.Length);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("电话:0755-12345678\n");
		PrintTransmit(bytes, bytes.Length);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("收银：01-店长\n");
		PrintTransmit(bytes, bytes.Length);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("时间：" + DateTime.Now.ToLocalTime().ToString() + "\n");
		PrintTransmit(bytes, bytes.Length);
		m_sbData = new StringBuilder("------------------------------");
		PrintString(m_sbData, 0);
		SetHTseat(new byte[3] { 12, 18, 26 }, 3);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("代码");
		PrintTransmit(bytes, bytes.Length);
		PrintNextHT();
		bytes = Encoding.GetEncoding("gb2312").GetBytes("单价");
		PrintTransmit(bytes, bytes.Length);
		PrintNextHT();
		bytes = Encoding.GetEncoding("gb2312").GetBytes("数量");
		PrintTransmit(bytes, bytes.Length);
		PrintNextHT();
		bytes = Encoding.GetEncoding("gb2312").GetBytes("金额\n");
		PrintTransmit(bytes, bytes.Length);
		m_sbData = new StringBuilder("48572819");
		PrintString(m_sbData, 1);
		PrintNextHT();
		m_sbData = new StringBuilder("2.00");
		PrintString(m_sbData, 1);
		PrintNextHT();
		m_sbData = new StringBuilder("3.00");
		PrintString(m_sbData, 1);
		PrintNextHT();
		m_sbData = new StringBuilder("6.00");
		PrintString(m_sbData, 0);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("怡宝矿泉水\n");
		PrintTransmit(bytes, bytes.Length);
		m_sbData = new StringBuilder("48572820");
		PrintString(m_sbData, 1);
		PrintNextHT();
		m_sbData = new StringBuilder("2.50");
		PrintString(m_sbData, 1);
		PrintNextHT();
		m_sbData = new StringBuilder("2.00");
		PrintString(m_sbData, 1);
		PrintNextHT();
		m_sbData = new StringBuilder("5.00");
		PrintString(m_sbData, 0);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("百事可乐(罐装)\n");
		PrintTransmit(bytes, bytes.Length);
		m_sbData = new StringBuilder("------------------------------");
		PrintString(m_sbData, 0);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("合计：");
		PrintTransmit(bytes, bytes.Length);
		PrintNextHT();
		PrintNextHT();
		m_sbData = new StringBuilder("5.00");
		PrintString(m_sbData, 1);
		PrintNextHT();
		m_sbData = new StringBuilder("11.00");
		PrintString(m_sbData, 0);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("优惠：");
		PrintTransmit(bytes, bytes.Length);
		PrintNextHT();
		PrintNextHT();
		PrintNextHT();
		m_sbData = new StringBuilder(" 0.00");
		PrintString(m_sbData, 0);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("应付：");
		PrintTransmit(bytes, bytes.Length);
		PrintNextHT();
		PrintNextHT();
		PrintNextHT();
		m_sbData = new StringBuilder("11.00");
		PrintString(m_sbData, 0);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("微信支付：");
		PrintTransmit(bytes, bytes.Length);
		PrintNextHT();
		PrintNextHT();
		PrintNextHT();
		m_sbData = new StringBuilder("11.00");
		PrintString(m_sbData, 0);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("找零：");
		PrintTransmit(bytes, bytes.Length);
		PrintNextHT();
		PrintNextHT();
		PrintNextHT();
		m_sbData = new StringBuilder(" 0.00");
		PrintString(m_sbData, 0);
		m_sbData = new StringBuilder("------------------------------");
		PrintString(m_sbData, 0);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("会员：\n");
		PrintTransmit(bytes, bytes.Length);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("券号：\n");
		PrintTransmit(bytes, bytes.Length);
		m_sbData = new StringBuilder("------------------------------");
		PrintString(m_sbData, 0);
		PrintFeedDot(20);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("网址：xxx.com.cn \n");
		PrintTransmit(bytes, bytes.Length);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("客户热线：400-8888-888\n");
		PrintTransmit(bytes, bytes.Length);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("微信号：wechat \n");
		PrintTransmit(bytes, bytes.Length);
		PrintFeedDot(20);
		m_sbData = new StringBuilder("http://xxx.com.cn");
		SetAlignment(1);
		PrintQrcodeII(m_sbData, m_sbData.ToString().Length, 6);
		PrintFeedDot(200);
		PrintCutpaper(1);
		SetReadZKmode(1);
		SetClean();
	}

	public void ExampleDemo2()
	{
		SetReadZKmode(0);
		byte[] bytes = Encoding.GetEncoding("gb2312").GetBytes("零售价：\n");
		PrintTransmit(bytes, bytes.Length);
		SetSizetext(3, 3);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("   ￥55.00\n");
		PrintTransmit(bytes, bytes.Length);
		SetSizetext(2, 2);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("品名：软中华盒装香烟\n");
		PrintTransmit(bytes, bytes.Length);
		SetSizetext(1, 1);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("-----------------------------------------------\n");
		PrintTransmit(bytes, bytes.Length);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("规格：150g 单位：盒 产地：深圳 代码： 123456789\n");
		PrintTransmit(bytes, bytes.Length);
		PrintFeedDot(20);
		m_sbData = new StringBuilder("123456789012");
		Print1Dbar(4, 72, 0, 2, 5, m_sbData);
		PrintFeedDot(20);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("                              价格举报：123456\n");
		PrintTransmit(bytes, bytes.Length);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("                       监督电话：0755-12345678\n");
		PrintTransmit(bytes, bytes.Length);
		PrintFeedDot(100);
		PrintCutpaper(1);
		SetSizetext(2, 2);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("品名:利智蓝莓（盒）\n");
		PrintTransmit(bytes, bytes.Length);
		PrintFeedDot(30);
		SetSizetext(1, 1);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("产地：利智         净含量≥125g\n");
		PrintTransmit(bytes, bytes.Length);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("等级：合格品       包装日期：2022.01.05\n");
		PrintTransmit(bytes, bytes.Length);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("进口商名称：XXXXXX\n");
		PrintTransmit(bytes, bytes.Length);
		bytes = Encoding.GetEncoding("gb2312").GetBytes("地址：深圳市宝安区XX产业园XX栋XX楼\n");
		PrintTransmit(bytes, bytes.Length);
		PrintFeedDot(20);
		m_sbData = new StringBuilder("12345678");
		Print1Dbar(4, 72, 1, 2, 3, m_sbData);
		PrintFeedDot(30);
		PrintFeedDot(100);
		PrintCutpaper(1);
		SetReadZKmode(1);
	}

	private void Example_PrintString()
	{
		try
		{
			m_sbData = new StringBuilder("Example_PrintString(Check)");
			PrintString(m_sbData, 0);
			m_sbData = new StringBuilder("1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqurstuvwxyz");
			for (int i = 0; i < 120; i++)
			{
				PrintString(m_sbData, 1);
			}
			PrintString(m_sbData, 0);
			int num = 0;
			byte[] array = new byte[3] { 29, 114, 1 };
			byte[] array2 = new byte[64];
			if (GetTransmit(array, array.Length, array2, array2.Length) == 0)
			{
				MessageBox.Show("Print incomplete!", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			else
			{
				MessageBox.Show("Printing complete!", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void Example_ITFCode()
	{
		try
		{
			m_sbData = new StringBuilder("Rotation Begin");
			PrintString(m_sbData, 0);
			SetRotation_Intomode();
			PrintRotation_Changeline();
			PrintRotation_Changeline();
			PrintRotation_Changeline();
			PrintRotation_Changeline();
			PrintRotation_Changeline();
			PrintRotation_Changeline();
			PrintRotation_Changeline();
			PrintRotation_Changeline();
			m_sbData = new StringBuilder("                  120600010007409577");
			PrintRotation_Sendtext(m_sbData, 0);
			m_sbData = new StringBuilder("                  ");
			PrintRotation_Sendtext(m_sbData, 1);
			m_sbData = new StringBuilder("120600010007409577");
			PrintRotation_Sendcode(0, 3, 3, 5, m_sbData);
			PrintRotation_Data();
			m_sbData = new StringBuilder("Rotation end");
			PrintString(m_sbData, 0);
			PrintFeedDot(90);
			PrintCutpaper(1);
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void button1_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) != 0)
			{
				return;
			}
			controlCharacterDisable();
			SetCommandmode(3);
			int num = 0;
			byte[] array = new byte[3];
			int num2 = 0;
			int num3 = 0;
			int num4 = -1;
			if (chkFontEpson.Checked)
			{
				num2 = m_strCodePage_Epson.GetUpperBound(0) + 1;
				num3 = 0;
			}
			else
			{
				num2 = comboBox10.SelectedIndex + 1;
				num3 = comboBox10.SelectedIndex;
			}
			for (; num3 < num2; num3++)
			{
				num = int.Parse(m_strCodePage_Epson[num3, 0]);
				array[0] = 27;
				array[1] = 116;
				array[2] = (byte)num;
				num4 = GetStatus();
				if (num4 == 0 || num4 == 8)
				{
					PrintTransmit(array, array.Length);
					PrintString(new StringBuilder("****" + m_strCodePage_Epson[num3, 1] + "****"), 0);
					PrintCharacterPage();
					Thread.Sleep(1200);
					continue;
				}
				MessageBox.Show(GetStringRes("R10008", "Printer status is abnormal!"), "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				break;
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		controlCharacterEnable();
	}

	private void button4_Click(object sender, EventArgs e)
	{
		int num = 0;
		int num2 = 0;
		string fileName;
		if (rdb_SendTypeHEX.Checked)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "BIN file|*.raw|BIN file|*.bin|BMP file|*.bmp|Txt file|*.txt|ALL files|*.*";
			openFileDialog.RestoreDirectory = true;
			openFileDialog.FilterIndex = 1;
			if (openFileDialog.ShowDialog() != DialogResult.OK)
			{
				return;
			}
			fileName = openFileDialog.FileName;
			if (fileName == null)
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder();
			int num3 = 0;
			if (fileName.ToLower().EndsWith(".bmp"))
			{
				byte[] array = new byte[16777216];
				num3 = GetBMPBufferDATAExt(new StringBuilder(fileName), array);
				for (num = 0; num < num3; num++)
				{
					num2 = (array[num] + 256) % 256;
					if (num2 < 16)
					{
						stringBuilder.Append("0" + $"{num2,1:X}" + " ");
					}
					else
					{
						stringBuilder.Append($"{num2,2:X}" + " ");
					}
				}
				tb_SendContentH.Text = stringBuilder.ToString();
				return;
			}
			if (fileName.ToLower().EndsWith(".txt"))
			{
				fileName = openFileDialog.FileName;
				if (fileName != null)
				{
					StreamReader streamReader = new StreamReader(fileName, Encoding.Default);
					string value = streamReader.ReadToEnd().TrimStart();
					if (!string.IsNullOrEmpty(value))
					{
						streamReader.Close();
					}
					tb_SendContentH.Text = value;
				}
				return;
			}
			FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
			num3 = (int)fileStream.Length;
			for (num = 0; num < num3; num++)
			{
				num2 = fileStream.ReadByte();
				if (num2 < 16)
				{
					stringBuilder.Append("0" + $"{num2,1:X}" + " ");
				}
				else
				{
					stringBuilder.Append($"{num2,2:X}" + " ");
				}
			}
			tb_SendContentH.Text = stringBuilder.ToString();
			fileStream.Close();
			return;
		}
		OpenFileDialog openFileDialog2 = new OpenFileDialog();
		openFileDialog2.Filter = "TXT file|*.txt|ALL files|*.*";
		openFileDialog2.RestoreDirectory = true;
		openFileDialog2.FilterIndex = 1;
		if (openFileDialog2.ShowDialog() != DialogResult.OK)
		{
			return;
		}
		fileName = openFileDialog2.FileName;
		if (fileName != null)
		{
			StreamReader streamReader = new StreamReader(fileName, Encoding.Default);
			string value = streamReader.ReadToEnd().TrimStart();
			if (!string.IsNullOrEmpty(value))
			{
				streamReader.Close();
			}
			tb_SendContentT.Text = value;
		}
	}

	private void button5_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) != 0)
			{
				return;
			}
			controlCharacterDisable();
			switch (int.Parse(cboFontLib.SelectedValue.ToString()))
			{
			case 1:
			{
				int num2 = 0;
				int num3 = 0;
				byte[] array4 = new byte[3];
				int num4 = 0;
				int num5 = -1;
				SetCommandmode(2);
				if (chkFontB.Checked)
				{
					num3 = m_strCodePage_FontB.GetUpperBound(0) + 1;
					num2 = 0;
				}
				else
				{
					num3 = comboBox9.SelectedIndex + 1;
					num2 = comboBox9.SelectedIndex;
				}
				for (; num2 < num3; num2++)
				{
					num4 = int.Parse(m_strCodePage_FontB[num2, 0]);
					array4[0] = 27;
					array4[1] = 33;
					array4[2] = (byte)num4;
					num5 = GetStatus();
					if (num5 == 0 || num5 == 8)
					{
						PrintTransmit(array4, array4.Length);
						PrintString(new StringBuilder("****" + m_strCodePage_FontB[num2, 1] + "****"), 0);
						PrintCharacterPage();
						Thread.Sleep(1200);
						continue;
					}
					MessageBox.Show(GetStringRes("R10008", "Printer status is abnormal!"), "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					break;
				}
				break;
			}
			case 2:
			{
				int num6 = int.Parse(cb_Character.SelectedValue.ToString());
				byte[] array = new byte[3] { 27, 121, 2 };
				PrintTransmit(array, array.Length);
				Thread.Sleep(200);
				int num7 = int.Parse(cb_Character.SelectedValue.ToString());
				byte[] array4 = new byte[3]
				{
					27,
					33,
					(byte)num7
				};
				PrintTransmit(array4, array4.Length);
				Thread.Sleep(200);
				PrintCharacterPage();
				break;
			}
			case 3:
			{
				int num = int.Parse(cb_Character.SelectedValue.ToString());
				byte[] array = new byte[3] { 19, 121, 3 };
				PrintTransmit(array, array.Length);
				Thread.Sleep(200);
				byte[] array2 = new byte[5] { 19, 116, 51, 85, 3 };
				PrintTransmit(array2, array2.Length);
				Thread.Sleep(2500);
				if (FunOpenPort(1) == 0)
				{
					byte[] array3 = new byte[5]
					{
						19,
						116,
						51,
						102,
						(byte)num
					};
					PrintTransmit(array3, array3.Length);
					Thread.Sleep(2500);
				}
				break;
			}
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		controlCharacterEnable();
	}

	private void btnFont2017All_Click(object sender, EventArgs e)
	{
		try
		{
			controlCharacterDisable();
			if (FunOpenPort(1) == 0)
			{
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				int num4 = -1;
				byte[] array = new byte[3];
				SetCommandmode(2);
				if (chkFont2017All.Checked)
				{
					num2 = m_strCodePage_2017ALL.GetUpperBound(0) + 1;
					num = 0;
				}
				else
				{
					num2 = comboBox8.SelectedIndex + 1;
					num = comboBox8.SelectedIndex;
				}
				for (; num < num2; num++)
				{
					num3 = int.Parse(m_strCodePage_2017ALL[num, 0]);
					array[0] = 27;
					array[1] = 116;
					array[2] = (byte)num3;
					num4 = GetStatus();
					if (num4 == 0 || num4 == 8)
					{
						PrintTransmit(array, array.Length);
						PrintString(new StringBuilder("****" + m_strCodePage_2017ALL[num, 1] + "****"), 0);
						PrintCharacterPage();
						Thread.Sleep(1200);
						continue;
					}
					MessageBox.Show(GetStringRes("R10008", "Printer status is abnormal!"), "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					break;
				}
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		controlCharacterEnable();
	}

	private void tb_bmpFilePath_DoubleClick(object sender, EventArgs e)
	{
		try
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "BMP file|*.bmp|JPG file|*.jpg;*.jpeg|PNG file|*.png";
			openFileDialog.RestoreDirectory = true;
			openFileDialog.FilterIndex = 1;
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				tb_bmpFilePath.Text = openFileDialog.FileName;
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void Tab_MultiFun_Load()
	{
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Expected O, but got Unknown
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Expected O, but got Unknown
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Expected O, but got Unknown
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Expected O, but got Unknown
		try
		{
			cboMulti1.Items.Clear();
			cboMulti1.Items.Add("1");
			cboMulti1.Items.Add("5");
			cboMulti1.Items.Add("10");
			cboMulti1.Items.Add("20");
			cboMulti1.Items.Add("100");
			cboMulti1.Items.Add("99999");
			cboMulti1.SelectedIndex = 0;
			if (m_cnn == null)
			{
				m_cnn = new SQLiteConnection("Data Source=dbbase.db3");
				((DbConnection)(object)m_cnn).Open();
				m_cmd = m_cnn.CreateCommand();
				m_sda_Comm = new SQLiteDataAdapter(m_cmd);
			}
			m_str_Sql = "CREATE TABLE IF NOT EXISTS [T_CmdInfo] ([F_Example_ID] int NOT NULL,[F_Example_Name]  VARCHAR(256) ,[F_Cmd_ID] int NOT NULL, [F_Select] TINYINT,[F_Hex] TINYINT, [F_Content] VARCHAR(16384), [F_Comment] VARCHAR(256), [F_Seq] int, [F_Sleep] int, CONSTRAINT [sqlite_autoindex_T_CmdInfo_1] PRIMARY KEY ([F_Example_ID],[F_Cmd_ID])) ";
			((DbCommand)(object)m_cmd).CommandText = m_str_Sql;
			((DbCommand)(object)m_cmd).ExecuteNonQuery();
			m_dsRS = new DataSet();
			int num = 0;
			int num2 = 0;
			DataSet dataSet = new DataSet();
			m_str_Sql = "select distinct F_Example_ID,F_Example_Name from T_CmdInfo order by F_Example_ID";
			((DbCommand)(object)m_cmd).CommandText = m_str_Sql;
			m_sda_Comm = new SQLiteDataAdapter(m_cmd);
			((DataAdapter)(object)m_sda_Comm).Fill(dataSet);
			num = dataSet.Tables[0].Rows.Count;
			cboMulti2.Items.Clear();
			for (num2 = num; num2 < 20; num2++)
			{
				m_str_Sql = string.Format("Insert into T_CmdInfo (F_Cmd_ID,F_Example_ID,F_Example_Name,F_Seq,F_Sleep,F_Hex)  values (1,{0},'Example{0}',10,0,0)", num2 + 1);
				((DbCommand)(object)m_cmd).CommandText = m_str_Sql;
				((DbCommand)(object)m_cmd).ExecuteNonQuery();
			}
			if (num < num2)
			{
				num = num2;
				m_str_Sql = "select distinct F_Example_ID,F_Example_Name from T_CmdInfo order by F_Example_ID";
				((DbCommand)(object)m_cmd).CommandText = m_str_Sql;
				m_sda_Comm = new SQLiteDataAdapter(m_cmd);
				dataSet.Clear();
				((DataAdapter)(object)m_sda_Comm).Fill(dataSet);
			}
			for (num2 = 0; num2 < num; num2++)
			{
				cboMulti2.Items.Add(dataSet.Tables[0].Rows[num2]["F_Example_Name"]);
			}
			cboMulti2.SelectedIndex = 0;
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void Tab_DoubleColor_Load()
	{
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Expected O, but got Unknown
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Expected O, but got Unknown
		try
		{
			if (m_cnn == null)
			{
				m_cnn = new SQLiteConnection("Data Source=dbbase.db3");
				((DbConnection)(object)m_cnn).Open();
				m_cmd = m_cnn.CreateCommand();
				m_sda_Comm = new SQLiteDataAdapter(m_cmd);
			}
			m_str_Sql = "CREATE TABLE IF NOT EXISTS [T_DCInfo] ([F_Example_ID] int NOT NULL,[F_Example_Name]  VARCHAR(256) ,[F_Cmd_ID] int NOT NULL, [F_Select] TINYINT,[F_Hex] TINYINT,[F_Color] TINYINT, [F_Content] VARCHAR(262144), [F_Comment] VARCHAR(256), [F_Seq] int, [F_Sleep] int, CONSTRAINT [sqlite_autoindex_T_DCInfo_1] PRIMARY KEY ([F_Example_ID],[F_Cmd_ID])) ";
			((DbCommand)(object)m_cmd).CommandText = m_str_Sql;
			((DbCommand)(object)m_cmd).ExecuteNonQuery();
			m_dsRS_DC = new DataSet();
			int num = 0;
			int num2 = 0;
			DataSet dataSet = new DataSet();
			m_str_Sql = "select distinct F_Example_ID,F_Example_Name from T_DCInfo order by F_Example_ID";
			((DbCommand)(object)m_cmd).CommandText = m_str_Sql;
			m_sda_Comm = new SQLiteDataAdapter(m_cmd);
			((DataAdapter)(object)m_sda_Comm).Fill(dataSet);
			num = dataSet.Tables[0].Rows.Count;
			cboDC01.Items.Clear();
			for (num2 = num; num2 < 20; num2++)
			{
				m_str_Sql = string.Format("Insert into T_DCInfo (F_Cmd_ID,F_Example_ID,F_Example_Name,F_Seq,F_Sleep,F_Hex)  values (1,{0},'Example{0}',10,0,0)", num2 + 1);
				((DbCommand)(object)m_cmd).CommandText = m_str_Sql;
				((DbCommand)(object)m_cmd).ExecuteNonQuery();
			}
			if (num < num2)
			{
				num = num2;
				m_str_Sql = "select distinct F_Example_ID,F_Example_Name from T_DCInfo order by F_Example_ID";
				((DbCommand)(object)m_cmd).CommandText = m_str_Sql;
				m_sda_Comm = new SQLiteDataAdapter(m_cmd);
				dataSet.Clear();
				((DataAdapter)(object)m_sda_Comm).Fill(dataSet);
			}
			for (num2 = 0; num2 < num; num2++)
			{
				cboDC01.Items.Add(dataSet.Tables[0].Rows[num2]["F_Example_Name"]);
			}
			cboDC01.SelectedIndex = 0;
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void Tab_NVBitmap_Load()
	{
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Expected O, but got Unknown
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected O, but got Unknown
		try
		{
			cboNVIndex.Items.Clear();
			for (int i = 0; i < 30; i++)
			{
				cboNVIndex.Items.Add(i + 1);
			}
			cboNVIndex.SelectedIndex = 0;
			if (m_cnn == null)
			{
				m_cnn = new SQLiteConnection("Data Source=dbbase.db3");
				((DbConnection)(object)m_cnn).Open();
				m_cmd = m_cnn.CreateCommand();
				m_sda_Comm = new SQLiteDataAdapter(m_cmd);
			}
			m_str_Sql = "CREATE TABLE IF NOT EXISTS [T_NVInfo] ([F_NV_ID] int NOT NULL, [F_NVIndex] int,[F_FilePath] VARCHAR(1024), [F_Comment] VARCHAR(256), CONSTRAINT [sqlite_autoindex_T_NVInfo_1] PRIMARY KEY ([F_NV_ID])) ";
			((DbCommand)(object)m_cmd).CommandText = m_str_Sql;
			((DbCommand)(object)m_cmd).ExecuteNonQuery();
			m_dsNV = new DataSet();
			m_str_Sql = "select * from T_NVInfo order by F_NVIndex";
			((DbCommand)(object)m_cmd).CommandText = m_str_Sql;
			m_sda_Comm = new SQLiteDataAdapter(m_cmd);
			((DataAdapter)(object)m_sda_Comm).Fill(m_dsNV);
			int count = m_dsNV.Tables[0].Rows.Count;
			for (int j = count; j < 30; j++)
			{
				m_dsNV.Tables[0].Rows.Add(j);
				m_dsNV.Tables[0].Rows[j]["F_NV_ID"] = j + 1;
				m_dsNV.Tables[0].Rows[j]["F_NVIndex"] = j + 1;
				m_dsNV.Tables[0].Rows[j]["F_FilePath"] = "";
				m_dsNV.Tables[0].Rows[j]["F_Comment"] = "";
			}
			dgvNV.DataSource = m_dsNV.Tables[0];
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
	{
		try
		{
			if (tabControl1.TabPages[tabControl1.SelectedIndex].Name == "tabPage2")
			{
				if (m_dsRS == null)
				{
					Tab_MultiFun_Load();
				}
			}
			else if (tabControl1.TabPages[tabControl1.SelectedIndex].Name == "tabPage4")
			{
				if (m_dsNV == null)
				{
					Tab_NVBitmap_Load();
				}
			}
			else if (tabControl1.TabPages[tabControl1.SelectedIndex].Name == "tabPage7" && m_dsRS_DC == null)
			{
				Tab_DoubleColor_Load();
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private bool CmdDataCheck()
	{
		bool result = false;
		try
		{
			m_dv = new DataView();
			m_dv.Table = m_dsRS.Tables[0];
			m_dv.RowFilter = "F_Select = true ";
			int count = m_dv.Count;
			if (count == 0)
			{
				MessageBox.Show("No command selected!", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return result;
			}
			int num = 0;
			string text = "";
			string text2 = "";
			int num2 = 0;
			for (num = 0; num < count; num++)
			{
				text = m_dv[num]["F_Content"].ToString();
				num2 = int.Parse(m_dv[num]["F_Cmd_ID"].ToString()) - 1;
				if (text.Equals(""))
				{
					MessageBox.Show("Content is empty", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					dgvRS.CurrentCell = dgvRS.Rows[num2].Cells["F_Content"];
					return result;
				}
				text2 = m_dv[num]["F_Hex"].ToString();
				if (text2.Equals("1"))
				{
					try
					{
						byte[] array = Utility.StrToHexByte(text);
					}
					catch (Exception)
					{
						MessageBox.Show(GetStringRes("R10003", "The input character format is wrong!"), "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						dgvRS.CurrentCell = dgvRS.Rows[num2].Cells["F_Content"];
						return result;
					}
				}
			}
			result = true;
		}
		catch (Exception ex2)
		{
			MessageBox.Show("Data check failure!\n" + ex2.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		return result;
	}

	private bool CmdDataSave()
	{
		bool result = false;
		int num = 0;
		SQLiteTransaction val = m_cnn.BeginTransaction();
		try
		{
			int iMulti2SelectIndex = m_iMulti2SelectIndex;
			string text = cboMulti2.Text.Trim();
			m_cmd.Transaction = val;
			m_str_Sql = "delete from T_CmdInfo Where F_Example_ID = " + iMulti2SelectIndex;
			((DbCommand)(object)m_cmd).CommandText = m_str_Sql;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			((DbCommand)(object)m_cmd).ExecuteNonQuery();
			int count = m_dsRS.Tables[0].Rows.Count;
			for (num = 0; num < count; num++)
			{
				num3 = (m_dsRS.Tables[0].Rows[num]["F_Select"].ToString().Equals("1") ? 1 : 0);
				num2 = (m_dsRS.Tables[0].Rows[num]["F_Hex"].ToString().Equals("1") ? 1 : 0);
				if (m_dsRS.Tables[0].Rows[num]["F_Seq"].ToString().Trim().Equals(""))
				{
					num4 += 10;
					m_dsRS.Tables[0].Rows[num]["F_Seq"] = num4;
				}
				else
				{
					num4 = int.Parse(m_dsRS.Tables[0].Rows[num]["F_Seq"].ToString());
				}
				if (m_dsRS.Tables[0].Rows[num]["F_Sleep"].ToString().Trim().Equals(""))
				{
					m_dsRS.Tables[0].Rows[num]["F_Sleep"] = "10";
				}
				m_str_Sql = string.Format("Insert into T_CmdInfo (F_Cmd_ID,F_Select,F_Hex,F_Content,F_Comment,F_Seq,F_Sleep,F_Example_ID,F_Example_Name)  values ({0},{1},{2},'{3}','{4}',{5},{6},{7},'{8}')", num + 1, num3, num2, m_dsRS.Tables[0].Rows[num]["F_Content"], m_dsRS.Tables[0].Rows[num]["F_Comment"], m_dsRS.Tables[0].Rows[num]["F_Seq"], m_dsRS.Tables[0].Rows[num]["F_Sleep"], iMulti2SelectIndex, text);
				((DbCommand)(object)m_cmd).CommandText = m_str_Sql;
				((DbCommand)(object)m_cmd).ExecuteNonQuery();
			}
			((DbTransaction)(object)val).Commit();
			cboMulti2.Items.Insert(iMulti2SelectIndex - 1, text);
			cboMulti2.SelectedIndex = iMulti2SelectIndex - 1;
			cboMulti2.Items.RemoveAt(iMulti2SelectIndex);
			result = true;
		}
		catch (Exception ex)
		{
			((DbTransaction)(object)val).Rollback();
			MessageBox.Show(GetStringRes("R10012", "Data save failure!\n") + ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		return result;
	}

	private void btnMulti1_Click(object sender, EventArgs e)
	{
		try
		{
			if (chkPrintIndex.Checked)
			{
				m_iChkIndex = 1;
			}
			else
			{
				m_iChkIndex = 0;
			}
			if (!CmdDataCheck() || !CmdDataSave() || FunOpenPort(1) != 0)
			{
				return;
			}
			Cursor = Cursors.WaitCursor;
			int iRowCount = m_dv.Count;
			m_dv.Sort = "F_Seq,F_Cmd_ID";
			int iIndex = 0;
			string strValue1 = "";
			int iValue1 = 0;
			int iPrintNums = 0;
			int iPrintIndex = 0;
			int iHex = 0;
			strValue1 = cboMulti1.Text;
			try
			{
				iPrintNums = int.Parse(strValue1);
				user1.Ptintimes = iPrintNums;
			}
			catch (Exception)
			{
				iPrintNums = 1;
			}
			Thread thread = new Thread((ThreadStart)delegate
			{
				int num = -1;
				string text = " ";
				user1.m_Switch = true;
				for (iPrintIndex = 0; iPrintIndex < iPrintNums; iPrintIndex++)
				{
					m_frmPrintingBox.UpdateRecvText2(3, GetStringRes("R10006", "Stop Printing"));
					if (!user1.m_Switch)
					{
						return;
					}
					Thread.Sleep(1500);
					switch (CheckPrinterStatus())
					{
					case 100:
						if (SetInit() != 0)
						{
							text = GetStringRes("R10015", "Printer is offline or no power");
							m_frmPrintingBox.UpdateRecvText2(2, text);
							user1.m_Switch = false;
							return;
						}
						break;
					case 0:
						text = GetStringRes("R10014", "Printer is ready");
						m_frmPrintingBox.UpdateRecvText2(2, text);
						break;
					case 1:
						text = GetStringRes("R10015", "Printer is offline or no power");
						m_frmPrintingBox.UpdateRecvText2(2, text);
						user1.m_Switch = false;
						return;
					case 2:
						text = GetStringRes("R10016", "Printer called unmatched library");
						m_frmPrintingBox.UpdateRecvText2(2, text);
						user1.m_Switch = false;
						return;
					case 3:
						text = GetStringRes("R10017", "Printer head is opened");
						m_frmPrintingBox.UpdateRecvText2(2, text);
						user1.m_Switch = false;
						return;
					case 4:
						text = GetStringRes("R10018", "Cutter is not reset");
						m_frmPrintingBox.UpdateRecvText2(2, text);
						user1.m_Switch = false;
						return;
					case 5:
						text = GetStringRes("R10019", "Printer head temp is abnormal");
						m_frmPrintingBox.UpdateRecvText2(2, text);
						user1.m_Switch = false;
						return;
					case 6:
						text = GetStringRes("R10020", "Printer does not detect blackmark");
						m_frmPrintingBox.UpdateRecvText2(2, text);
						user1.m_Switch = false;
						return;
					case 7:
						text = GetStringRes("R10021", "Paper\u00a0out");
						m_frmPrintingBox.UpdateRecvText2(2, text);
						user1.m_Switch = false;
						return;
					case 8:
						text = GetStringRes("R10022", "Paper\u00a0low");
						m_frmPrintingBox.UpdateRecvText2(2, text);
						break;
					}
					m_sbData = new StringBuilder("Index:" + (iPrintIndex + 1));
					if (m_iChkIndex == 1)
					{
						PrintString(m_sbData);
					}
					m_frmPrintingBox.UpdateRecvText2(1, (iPrintIndex + 1).ToString());
					for (iIndex = 0; iIndex < iRowCount; iIndex++)
					{
						strValue1 = m_dv[iIndex]["F_Hex"].ToString();
						try
						{
							if (m_dv[iIndex]["F_Hex"].ToString().Equals(""))
							{
								iHex = 0;
							}
							else
							{
								iHex = short.Parse(m_dv[iIndex]["F_Hex"].ToString());
							}
						}
						catch (Exception)
						{
							iHex = 0;
						}
						strValue1 = m_dv[iIndex]["F_Content"].ToString();
						if (iHex == 0)
						{
							PrintString(new StringBuilder(strValue1));
						}
						else
						{
							byte[] array = Utility.StrToHexByte(strValue1);
							PrintTransmit(array, array.Length);
						}
						strValue1 = m_dv[iIndex]["F_Sleep"].ToString();
						iValue1 = int.Parse(m_dv[iIndex]["F_Sleep"].ToString());
						Thread.Sleep(iValue1);
					}
					if (m_iDevType != 4)
					{
						Thread.Sleep(1000);
					}
				}
				m_frmPrintingBox.UpdateRecvText2(3, GetStringRes("R10007", "OK"));
				user1.m_Switch = true;
			});
			thread.Start();
			m_frmPrintingBox.ShowDialog();
			thread.Abort();
			user1.m_Switch = false;
		}
		catch (Exception ex2)
		{
			MessageBox.Show(ex2.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		Cursor = Cursors.Default;
	}

	private void dgvRS_DataError(object sender, DataGridViewDataErrorEventArgs e)
	{
		MessageBox.Show(GetStringRes("R10013", "Data error!"), "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
	}

	private void btnNVPrint_Click(object sender, EventArgs e)
	{
		if (FunOpenPort(1) == 0)
		{
			PrintNvmbp(cboNVIndex.SelectedIndex + 1, 1);
		}
	}

	private void cboMulti2_SelectedIndexChanged(object sender, EventArgs e)
	{
		try
		{
			ComboBox comboBox = (ComboBox)sender;
			m_iMulti2SelectIndex = comboBox.SelectedIndex + 1;
			FillMultiExample(m_iMulti2SelectIndex);
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void FillMultiExample(int iExample_ID)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected O, but got Unknown
		m_dsRS = new DataSet();
		m_str_Sql = $"select F_Cmd_ID,F_Select,F_Hex,F_Content,F_Comment,F_Sleep,F_Seq from T_CmdInfo  Where F_Example_ID = {iExample_ID} order by F_Seq";
		((DbCommand)(object)m_cmd).CommandText = m_str_Sql;
		m_sda_Comm = new SQLiteDataAdapter(m_cmd);
		((DataAdapter)(object)m_sda_Comm).Fill(m_dsRS);
		int count = m_dsRS.Tables[0].Rows.Count;
		for (int i = count; i < 100; i++)
		{
			m_dsRS.Tables[0].Rows.Add(i);
			m_dsRS.Tables[0].Rows[i]["F_Cmd_ID"] = i + 1;
			m_dsRS.Tables[0].Rows[i]["F_Select"] = 0;
			m_dsRS.Tables[0].Rows[i]["F_Hex"] = 0;
			m_dsRS.Tables[0].Rows[i]["F_Sleep"] = 10;
			m_dsRS.Tables[0].Rows[i]["F_Seq"] = (i + 1) * 10;
		}
		dgvRS.DataSource = m_dsRS.Tables[0];
	}

	public int CheckPrinterStatus()
	{
		if (m_iDevType == 4)
		{
			return 100;
		}
		Thread.Sleep(200);
		m_iStatus = GetStatus();
		if (m_iStatus == 1)
		{
			m_iStatus = GetStatus();
		}
		return m_iStatus;
	}

	private void dgvNV_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
	{
		try
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "BMP file|*.bmp";
			openFileDialog.RestoreDirectory = true;
			openFileDialog.FilterIndex = 1;
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				dgvNV.Rows[e.RowIndex].Cells["F_FilePath"].Value = openFileDialog.FileName;
				btnSetNV.Focus();
				dgvNV.Focus();
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private int NVDataCheck()
	{
		int result = -1;
		try
		{
			int count = m_dsNV.Tables[0].Rows.Count;
			int num = 0;
			string text = "";
			for (num = 0; num < count; num++)
			{
				text = m_dsNV.Tables[0].Rows[num]["F_FilePath"].ToString();
				if (text.Trim().Equals(""))
				{
					break;
				}
				if (!File.Exists(text))
				{
					dgvNV.CurrentCell = dgvNV["F_FilePath", num];
					throw new Exception("Wrong file path!");
				}
			}
			result = num;
		}
		catch (Exception ex)
		{
			MessageBox.Show("Data check failure!\n" + ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		return result;
	}

	private bool NVDataSave()
	{
		bool result = false;
		int num = 0;
		SQLiteTransaction val = m_cnn.BeginTransaction();
		try
		{
			m_cmd.Transaction = val;
			m_str_Sql = "delete from T_NVInfo ";
			((DbCommand)(object)m_cmd).CommandText = m_str_Sql;
			((DbCommand)(object)m_cmd).ExecuteNonQuery();
			int count = m_dsNV.Tables[0].Rows.Count;
			for (num = 0; num < count; num++)
			{
				m_str_Sql = string.Format("Insert into T_NVInfo (F_NV_ID,F_NVIndex,F_FilePath,F_Comment)  values ({0},{1},'{2}','{3}')", num + 1, m_dsNV.Tables[0].Rows[num]["F_NVIndex"], m_dsNV.Tables[0].Rows[num]["F_FilePath"], m_dsNV.Tables[0].Rows[num]["F_Comment"]);
				((DbCommand)(object)m_cmd).CommandText = m_str_Sql;
				((DbCommand)(object)m_cmd).ExecuteNonQuery();
			}
			((DbTransaction)(object)val).Commit();
			result = true;
		}
		catch (Exception ex)
		{
			((DbTransaction)(object)val).Rollback();
			MessageBox.Show("Data save failure!\n" + ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		return result;
	}

	private void btnSetNV_Click(object sender, EventArgs e)
	{
		try
		{
			int num = NVDataCheck();
			if (num < 1)
			{
				MessageBox.Show(GetStringRes("R10009", "Please set NV Bitmap file path"), "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			if (!NVDataSave())
			{
				return;
			}
			SetPrintportFlowCtrl(1);
			if (FunOpenPort(1) != 0)
			{
				SetPrintportFlowCtrl(0);
				return;
			}
			Cursor = Cursors.WaitCursor;
			StringBuilder stringBuilder = new StringBuilder("");
			int num2 = 0;
			for (num2 = 0; num2 < num; num2++)
			{
				stringBuilder.Append(string.Concat(m_dsNV.Tables[0].Rows[num2]["F_FilePath"], ";"));
			}
			int num3 = SetNvbmp(num, stringBuilder);
			Cursor = Cursors.Default;
			if (num3 == 0)
			{
				MessageBox.Show(GetStringRes("R10010", "Set NVBitmap successfully!"), "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
			else
			{
				MessageBox.Show(GetStringRes("R10011", "Set NVBitmap failed!"), "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
		catch (Exception ex)
		{
			Cursor = Cursors.Default;
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		SetPrintportFlowCtrl(0);
	}

	private void cboPort_SelectedIndexChanged(object sender, EventArgs e)
	{
		try
		{
			bool enabled = !cboPort.Text.StartsWith("LPT");
			bt_GetProductMessage.Enabled = enabled;
			bt_GetStatus.Enabled = enabled;
			enabled = cboPort.Text.StartsWith("Network");
			lblPrintConnValue2.Visible = enabled;
			cboPrintConnValue2.Visible = enabled;
			enabled = cboPort.Text.StartsWith("COM");
			lblPrintConnValue1.Visible = enabled;
			cboBandrate.Visible = enabled;
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void btnWIFISet1_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) == 0)
			{
				byte[] array = new byte[5];
				int num = 0;
				array[num++] = 19;
				array[num++] = 146;
				array[num++] = 2;
				array[num++] = 1;
				array[num++] = (byte)cboWIFIModel.SelectedIndex;
				PrintTransmit(array, array.Length);
				try
				{
					Settings.Default.wifiModel = cboWIFIModel.SelectedIndex;
					Settings.Default.Save();
					return;
				}
				catch (Exception)
				{
					return;
				}
			}
		}
		catch (Exception ex2)
		{
			MessageBox.Show(ex2.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void TabWifi_Load()
	{
		try
		{
			txtWifiWan.Text = Settings.Default.setWifiWan;
			txtWifiPassword.Text = Settings.Default.setWifiPassword;
			cboWIFIModel.Items.Add("TCP/DHCP");
			cboWIFIModel.Items.Add("MQTT/DHCP");
			cboWIFIModel.Items.Add("TCP/STATIC");
			cboWIFIModel.Items.Add("MQTT/STATIC");
			int num = Settings.Default.wifiModel;
			if (num < cboWIFIModel.Items.Count)
			{
				num = 0;
			}
			cboWIFIModel.SelectedIndex = num;
			txtWifiIP.Text = Settings.Default.setIP;
			txtWifiPort.Text = Settings.Default.setPort;
			txtWifiMQTTClientID.Text = Settings.Default.setWifiClientID;
			txtWifiMQTTClientName.Text = Settings.Default.setWifiClientName;
			txtWifiMQTTPassword.Text = Settings.Default.setWifiClientPassword;
			txtWifiMQTTIP.Text = Settings.Default.setWifiMQTTIP;
			txtWifiMQTTPort.Text = Settings.Default.setWifiMQTTPort;
			txtWifiPublish1.Text = Settings.Default.setWifiPublish1;
			txtWifiPublish2.Text = Settings.Default.setWifiPublish2;
			txtWifiPublish3.Text = Settings.Default.setWifiPublish3;
			txtWifiSubscribe1.Text = Settings.Default.setWifiSubscribe1;
			txtWifiSubscribe2.Text = Settings.Default.setWifiSubscribe2;
			txtWifiSubscribe3.Text = Settings.Default.setWifiSubscribe3;
			txtWifiIPAddr.Text = Settings.Default.setWifiIPAddr;
			txtWifiMask.Text = Settings.Default.setWifiMask;
			txtWifiGateway.Text = Settings.Default.setWifiGateway;
			txtWifiDNS.Text = Settings.Default.setWifiDNS;
			txtSpeedData.Text = Settings.Default.SpeedData;
			txtSpeedCount.Text = Settings.Default.SpeedCount;
		}
		catch (Exception)
		{
		}
	}

	private void btnWifiSet2_Click(object sender, EventArgs e)
	{
		string text = txtWifiWan.Text.Trim();
		if (text.Equals(""))
		{
			MessageBox.Show("Invalid WLan!", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			txtWifiWan.Focus();
			return;
		}
		string text2 = txtWifiPassword.Text.Trim();
		if (text2.Equals(""))
		{
			MessageBox.Show("Invalid Password!", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			txtWifiPassword.Focus();
			return;
		}
		try
		{
			if (FunOpenPort(1) == 0)
			{
				string text3 = $"AT+CWJAP=\"{text}\",\"{text2}\"\r\n";
				byte[] bytes = Encoding.ASCII.GetBytes("  " + text3);
				bytes[0] = 19;
				bytes[1] = 146;
				PrintTransmit(bytes, bytes.Length);
				try
				{
					Settings.Default.setWifiWan = text;
					Settings.Default.setWifiPassword = text2;
					Settings.Default.Save();
					return;
				}
				catch (Exception)
				{
					return;
				}
			}
		}
		catch (Exception ex2)
		{
			MessageBox.Show(ex2.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void btn_SetInit_Click(object sender, EventArgs e)
	{
		if (m_iInit == 0)
		{
			FunClosePort();
		}
		else if (FunOpenPort(1) != 0)
		{
		}
	}

	private void btnWifiSet3_Click(object sender, EventArgs e)
	{
		string text = txtWifiIP.Text.Trim();
		if (text == "")
		{
			MessageBox.Show("Invalid IP address", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			txtWifiIP.Focus();
			return;
		}
		int num = 0;
		string text2 = "";
		try
		{
			text2 = ((int)Convert.ToUInt16(txtWifiPort.Text)).ToString();
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message.ToString(), Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			txtWifiPort.Focus();
			return;
		}
		try
		{
			if (FunOpenPort(1) == 0)
			{
				byte[] bytes = Encoding.ASCII.GetBytes(text);
				byte[] bytes2 = Encoding.ASCII.GetBytes(text2);
				byte[] array = new byte[bytes.Length + bytes2.Length + 10];
				int num2 = 0;
				array[num2++] = 19;
				array[num2++] = 146;
				array[num2++] = 2;
				array[num2++] = 17;
				for (int i = 0; i < bytes.Length; i++)
				{
					array[num2++] = bytes[i];
				}
				array[num2++] = 0;
				array[num2++] = 19;
				array[num2++] = 146;
				array[num2++] = 2;
				array[num2++] = 33;
				for (int i = 0; i < bytes2.Length; i++)
				{
					array[num2++] = bytes2[i];
				}
				array[num2++] = 0;
				PrintTransmit(array, array.Length);
				try
				{
					Settings.Default.setIP = text;
					Settings.Default.setPort = txtWifiPort.Text;
					Settings.Default.Save();
					return;
				}
				catch (Exception)
				{
					return;
				}
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void btnWifiSet4_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) != 0)
			{
				return;
			}
			string text = txtWifiMQTTClientID.Text.Trim();
			if (text.Equals(""))
			{
				MessageBox.Show("Invalid Client ID!", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				txtWifiMQTTClientID.Focus();
				return;
			}
			string text2 = txtWifiMQTTClientName.Text.Trim();
			if (text2.Equals(""))
			{
				MessageBox.Show("Invalid Client Name!", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				txtWifiMQTTClientName.Focus();
				return;
			}
			string text3 = txtWifiMQTTPassword.Text.Trim();
			if (text3.Equals(""))
			{
				MessageBox.Show("Invalid Password!", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				txtWifiMQTTPassword.Focus();
				return;
			}
			byte[] bytes = Encoding.ASCII.GetBytes(text);
			byte[] bytes2 = Encoding.ASCII.GetBytes(text2);
			byte[] bytes3 = Encoding.ASCII.GetBytes(text3);
			byte[] array = new byte[bytes.Length + bytes2.Length + bytes3.Length + 15];
			int num = 0;
			array[num++] = 19;
			array[num++] = 147;
			array[num++] = 2;
			array[num++] = 1;
			for (int i = 0; i < bytes.Length; i++)
			{
				array[num++] = bytes[i];
			}
			array[num++] = 0;
			array[num++] = 19;
			array[num++] = 147;
			array[num++] = 2;
			array[num++] = 2;
			for (int i = 0; i < bytes2.Length; i++)
			{
				array[num++] = bytes2[i];
			}
			array[num++] = 0;
			array[num++] = 19;
			array[num++] = 147;
			array[num++] = 2;
			array[num++] = 3;
			for (int i = 0; i < bytes3.Length; i++)
			{
				array[num++] = bytes3[i];
			}
			array[num++] = 0;
			PrintTransmit(array, array.Length);
			try
			{
				Settings.Default.setWifiClientID = text;
				Settings.Default.setWifiClientName = text2;
				Settings.Default.setWifiClientPassword = text3;
				Settings.Default.Save();
			}
			catch (Exception)
			{
			}
		}
		catch (Exception ex2)
		{
			MessageBox.Show(ex2.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void btnWifiSet5_Click(object sender, EventArgs e)
	{
		try
		{
			int num = 0;
			if (FunOpenPort(1) != 0)
			{
				return;
			}
			string text = txtWifiPublish1.Text.Trim();
			string text2 = txtWifiPublish2.Text.Trim();
			string text3 = txtWifiPublish3.Text.Trim();
			if (text.Equals("") && text2.Equals("") && text3.Equals(""))
			{
				MessageBox.Show("Invalid Publish!", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				txtWifiPublish1.Focus();
				return;
			}
			string text4 = txtWifiSubscribe1.Text.Trim();
			string text5 = txtWifiSubscribe2.Text.Trim();
			string text6 = txtWifiSubscribe3.Text.Trim();
			if (text4.Equals("") && text5.Equals("") && text6.Equals(""))
			{
				MessageBox.Show("Invalid Subscribe!", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				txtWifiSubscribe1.Focus();
				return;
			}
			string text7 = "";
			if (!text.Equals(""))
			{
				text7 = text7 + text + ";";
			}
			if (!text2.Equals(""))
			{
				text7 = text7 + text2 + ";";
			}
			if (!text3.Equals(""))
			{
				text7 = text7 + text3 + ";";
			}
			string text8 = "";
			if (!text4.Equals(""))
			{
				text8 = text8 + text4 + ";";
			}
			if (!text5.Equals(""))
			{
				text8 = text8 + text5 + ";";
			}
			if (!text6.Equals(""))
			{
				text8 = text8 + text6 + ";";
			}
			byte[] bytes = Encoding.ASCII.GetBytes(text7);
			byte[] bytes2 = Encoding.ASCII.GetBytes(text8);
			byte[] array = new byte[bytes.Length + 5 + bytes2.Length + 5];
			array[num++] = 19;
			array[num++] = 147;
			array[num++] = 2;
			array[num++] = 33;
			for (int i = 0; i < bytes.Length; i++)
			{
				array[num++] = bytes[i];
			}
			array[num++] = 0;
			array[num++] = 19;
			array[num++] = 147;
			array[num++] = 2;
			array[num++] = 34;
			for (int i = 0; i < bytes2.Length; i++)
			{
				array[num++] = bytes2[i];
			}
			array[num++] = 0;
			PrintTransmit(array, array.Length);
			try
			{
				Settings.Default.setWifiPublish1 = text;
				Settings.Default.setWifiPublish2 = text2;
				Settings.Default.setWifiPublish3 = text3;
				Settings.Default.setWifiSubscribe1 = text4;
				Settings.Default.setWifiSubscribe2 = text5;
				Settings.Default.setWifiSubscribe3 = text6;
				Settings.Default.Save();
			}
			catch (Exception)
			{
			}
		}
		catch (Exception ex2)
		{
			MessageBox.Show(ex2.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void btnWifiSet6_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) != 0)
			{
				return;
			}
			string text = txtWifiPublish1.Text.Trim();
			string text2 = txtWifiPublish2.Text.Trim();
			string text3 = txtWifiPublish3.Text.Trim();
			if (text.Equals("") && text2.Equals("") && text3.Equals(""))
			{
				MessageBox.Show("Invalid Publish!", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				txtWifiPublish1.Focus();
				return;
			}
			string text4 = "";
			if (!text.Equals(""))
			{
				text4 = text4 + text + ";";
			}
			if (!text2.Equals(""))
			{
				text4 = text4 + text2 + ";";
			}
			if (!text3.Equals(""))
			{
				text4 = text4 + text3 + ";";
			}
			byte[] bytes = Encoding.ASCII.GetBytes(text4);
			byte[] array = new byte[bytes.Length + 5];
			int num = 0;
			array[num++] = 19;
			array[num++] = 147;
			array[num++] = 2;
			array[num++] = 33;
			for (int i = 0; i < bytes.Length; i++)
			{
				array[num++] = bytes[i];
			}
			array[num++] = 0;
			PrintTransmit(array, array.Length);
			try
			{
				Settings.Default.setWifiPublish1 = text;
				Settings.Default.setWifiPublish2 = text2;
				Settings.Default.setWifiPublish3 = text3;
				Settings.Default.Save();
			}
			catch (Exception)
			{
			}
		}
		catch (Exception ex2)
		{
			MessageBox.Show(ex2.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void btnWifiIPAddr_Click(object sender, EventArgs e)
	{
		string ipString = txtWifiIPAddr.Text.Trim();
		if (IPAddress.TryParse(ipString, out var address))
		{
			ipString = address.ToString();
			try
			{
				if (FunOpenPort(1) == 0)
				{
					string[] array = ipString.Split('.');
					byte[] array2 = new byte[4]
					{
						byte.Parse(array[0]),
						byte.Parse(array[1]),
						byte.Parse(array[2]),
						byte.Parse(array[3])
					};
					byte[] array3 = new byte[array2.Length + 4];
					int num = 0;
					array3[num++] = 19;
					array3[num++] = 146;
					array3[num++] = 3;
					array3[num++] = 1;
					for (int i = 0; i < array2.Length; i++)
					{
						array3[num++] = array2[i];
					}
					PrintTransmit(array3, array3.Length);
					try
					{
						Settings.Default.setWifiIPAddr = ipString;
						Settings.Default.Save();
						return;
					}
					catch (Exception)
					{
						return;
					}
				}
				return;
			}
			catch (Exception ex2)
			{
				MessageBox.Show(ex2.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
		}
		MessageBox.Show("Invalid IP address!", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		txtWifiIPAddr.Focus();
	}

	private void btnWifiMask_Click(object sender, EventArgs e)
	{
		string ipString = txtWifiMask.Text.Trim();
		if (IPAddress.TryParse(ipString, out var address))
		{
			ipString = address.ToString();
			try
			{
				if (FunOpenPort(1) == 0)
				{
					string[] array = ipString.Split('.');
					byte[] array2 = new byte[4]
					{
						byte.Parse(array[0]),
						byte.Parse(array[1]),
						byte.Parse(array[2]),
						byte.Parse(array[3])
					};
					byte[] array3 = new byte[array2.Length + 4];
					int num = 0;
					array3[num++] = 19;
					array3[num++] = 146;
					array3[num++] = 3;
					array3[num++] = 2;
					for (int i = 0; i < array2.Length; i++)
					{
						array3[num++] = array2[i];
					}
					PrintTransmit(array3, array3.Length);
					try
					{
						Settings.Default.setWifiMask = ipString;
						Settings.Default.Save();
						return;
					}
					catch (Exception)
					{
						return;
					}
				}
				return;
			}
			catch (Exception ex2)
			{
				MessageBox.Show(ex2.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
		}
		MessageBox.Show("Invalid Subnet mask!", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		txtWifiMask.Focus();
	}

	private void btnWifiGateway_Click(object sender, EventArgs e)
	{
		string ipString = txtWifiGateway.Text.Trim();
		if (IPAddress.TryParse(ipString, out var address))
		{
			ipString = address.ToString();
			try
			{
				if (FunOpenPort(1) == 0)
				{
					string[] array = ipString.Split('.');
					byte[] array2 = new byte[4]
					{
						byte.Parse(array[0]),
						byte.Parse(array[1]),
						byte.Parse(array[2]),
						byte.Parse(array[3])
					};
					byte[] array3 = new byte[array2.Length + 4];
					int num = 0;
					array3[num++] = 19;
					array3[num++] = 146;
					array3[num++] = 3;
					array3[num++] = 3;
					for (int i = 0; i < array2.Length; i++)
					{
						array3[num++] = array2[i];
					}
					PrintTransmit(array3, array3.Length);
					try
					{
						Settings.Default.setWifiGateway = ipString;
						Settings.Default.Save();
						return;
					}
					catch (Exception)
					{
						return;
					}
				}
				return;
			}
			catch (Exception ex2)
			{
				MessageBox.Show(ex2.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
		}
		MessageBox.Show("Invalid Gateway!", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		txtWifiGateway.Focus();
	}

	private void btnWifiDNS_Click(object sender, EventArgs e)
	{
		string ipString = txtWifiDNS.Text.Trim();
		if (IPAddress.TryParse(ipString, out var address))
		{
			ipString = address.ToString();
			try
			{
				if (FunOpenPort(1) == 0)
				{
					string[] array = ipString.Split('.');
					byte[] array2 = new byte[4]
					{
						byte.Parse(array[0]),
						byte.Parse(array[1]),
						byte.Parse(array[2]),
						byte.Parse(array[3])
					};
					byte[] array3 = new byte[array2.Length + 4];
					int num = 0;
					array3[num++] = 19;
					array3[num++] = 146;
					array3[num++] = 3;
					array3[num++] = 4;
					for (int i = 0; i < array2.Length; i++)
					{
						array3[num++] = array2[i];
					}
					PrintTransmit(array3, array3.Length);
					try
					{
						Settings.Default.setWifiDNS = ipString;
						Settings.Default.Save();
						return;
					}
					catch (Exception)
					{
						return;
					}
				}
				return;
			}
			catch (Exception ex2)
			{
				MessageBox.Show(ex2.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
		}
		MessageBox.Show("Invalid DNS!", Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		txtWifiDNS.Focus();
	}

	private void btnWifiRead_Click(object sender, EventArgs e)
	{
		try
		{
			int num = 0;
			byte[] array = new byte[4] { 19, 146, 3, 0 };
			byte[] array2 = new byte[64];
			if (FunOpenPort(1) != 0)
			{
				return;
			}
			num = GetTransmit(array, array.Length, array2, array2.Length);
			if (num < 16)
			{
				MessageBox.Show("Failure!", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			string text = "";
			int num2 = 0;
			for (num2 = 0; num2 < 3; num2++)
			{
				text = text + array2[num2] + ".";
			}
			text += array2[num2++];
			txtWifiIPAddr.Text = text;
			text = "";
			for (; num2 < 7; num2++)
			{
				text = text + array2[num2] + ".";
			}
			text += array2[num2++];
			txtWifiMask.Text = text;
			text = "";
			for (; num2 < 11; num2++)
			{
				text = text + array2[num2] + ".";
			}
			text += array2[num2++];
			txtWifiGateway.Text = text;
			text = "";
			for (; num2 < 15; num2++)
			{
				text = text + array2[num2] + ".";
			}
			text += array2[num2++];
			txtWifiDNS.Text = text;
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void button7_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) == 0)
			{
				StringBuilder stringBuilder = new StringBuilder("                                 ");
				if (GetPrintIDorName(stringBuilder) == 0)
				{
					textBox1.Text = stringBuilder.ToString().Trim();
				}
				else
				{
					MessageBox.Show("GetPrintIDorName failure!", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void button6_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) != 0)
			{
				return;
			}
			if (textBox1.Text.Trim() == "")
			{
				MessageBox.Show("Printer ID is empty!", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				button7.Focus();
				button7_Click(sender, e);
				return;
			}
			StringBuilder printIDorName = new StringBuilder(textBox1.Text.Trim());
			if (SetPrintIDorName(printIDorName) == 0)
			{
				Thread.Sleep(1200);
			}
			else
			{
				MessageBox.Show("SetPrintIDorName failure!", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void cboCodePage_Load()
	{
		try
		{
			string[,] array = new string[5, 2]
			{
				{ "437", "437" },
				{ "874", "windows-874" },
				{ "932", "932" },
				{ "936", "936" },
				{ "1256", "windows-1256" }
			};
			DataTable dataTable = new DataTable();
			DataColumn dataColumn = new DataColumn();
			dataColumn.DataType = Type.GetType("System.Int32");
			dataColumn.ColumnName = "id";
			dataTable.Columns.Add(dataColumn);
			dataColumn = new DataColumn();
			dataColumn.DataType = Type.GetType("System.String");
			dataColumn.ColumnName = "name";
			dataTable.Columns.Add(dataColumn);
			int num = array.GetUpperBound(0) + 1;
			int num2 = 0;
			for (num2 = 0; num2 < num; num2++)
			{
				DataRow dataRow = dataTable.NewRow();
				dataRow["id"] = array[num2, 0];
				dataRow["name"] = array[num2, 1];
				dataTable.Rows.Add(dataRow);
			}
			cboCodePage.DataSource = dataTable;
			cboCodePage.DisplayMember = "name";
			cboCodePage.ValueMember = "id";
			if (user1.m_strLanguage == GlobalVar.g_str_Language_zh_CHS)
			{
				cboCodePage.SelectedIndex = 3;
			}
			else
			{
				cboCodePage.SelectedIndex = 0;
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void cboCodePage_SelectedIndexChanged(object sender, EventArgs e)
	{
		try
		{
			if (tb_SendContentT.Visible && tb_SendContentT.Text.Trim() == "")
			{
				switch (int.Parse(cboCodePage.SelectedValue.ToString()))
				{
				case 437:
					tb_SendContentT.Text = "byte[] bSend = Encoding.GetEncoding(CodePageID).GetBytes(\"character string\");\r\nPrintTransmit(bSend, bSend.Length);\r\n";
					break;
				case 932:
					tb_SendContentT.Text = "サーマルレシートプリンターの印刷情報\r\n納税者番号：123456789\r\nID情報：ABCユーザー：いいえ1\r\n数量：1アイテム\r\nテストページ1\r\n\r\n";
					break;
				case 874:
					tb_SendContentT.Text = "เซ\u0e47นทร\u0e31ลแฟม\u0e34ล\u0e35\u0e48มาร\u0e4cท (4440) ซ\u0e35เอฟเอ\u0e47ม ออฟฟ\u0e34ศ\r\nTAX ID 0105535133093 (VAT Included)\r\nใบเสร\u0e47จร\u0e31บเง\u0e34น/ใบกำก\u0e31บภาษ\u0e35อย\u0e48างย\u0e48อ\r\nID:SCO120000203129 USR:002\r\n2021/03/29 16:43 BNO:S2104440002-0000004\r\n1 แฟมม\u0e34หม\u0e31\u0e48นโถว 50กร\u0e31ม                  7.00V\r\nTOTAL(QTY) 1 Items                  5.00\r\ntest ลด5บาท                          5.00\r\ntest ลด5บาท                          5.00\r\nรห\u0e31สสมาช\u0e34ก : 2011010003746837 นาย\r\nทดสอบ คะแนนสะสมคงเหล\u0e37อ :50          \r\nพ\u0e34เศษแลกซ\u0e37\u0e49อพร\u0e35เม\u0e35\u0e48ยมเฉพาะT1   \r\n** ขอบค\u0e38ณท\u0e35\u0e48มาอ\u0e38ดหน\u0e38น โทร 02-660-1000 **\r\n";
					break;
				case 1256:
					tb_SendContentT.Text = "تفاحتان ، 3 موزات ، السعر الإجمالي\r\n";
					tb_SendContentT.Text = "جمهوری اسلامی ایران\r\nخوش آمدید\r\nی ه و ن م ل گ ک ق ف غ ع ظ ط ض ص ش س ژ ز ر ذ د خ ح چ ج ث ت پ ب ا ء\r\n\r\n";
					break;
				}
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void btnFontDownload_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) != 0)
			{
				return;
			}
			string text = "";
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Bin File (*.bin)|*.bin|All files (*.*)|*.*";
			openFileDialog.RestoreDirectory = true;
			openFileDialog.FilterIndex = 1;
			if (openFileDialog.ShowDialog() != DialogResult.OK)
			{
				return;
			}
			text = openFileDialog.FileName;
			txtFontPath.Text = text;
			if (text == "")
			{
				MessageBox.Show("Please select a font library file!", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			byte[] array = new byte[4096];
			byte[] array2 = File.ReadAllBytes(text);
			int num = array2.Length;
			int num2 = num % 65536;
			int num3 = (num - num2) / 65536;
			for (int i = 0; i < num3; i++)
			{
				int iLength = 0;
				array[iLength++] = 18;
				array[iLength++] = 221;
				array[iLength++] = (byte)i;
				PrintTransmit(array, iLength);
				Thread.Sleep(300);
				int num4 = i * 65536;
				for (int j = 0; j < 16; j++)
				{
					iLength = 0;
					for (int k = 0; k < 4096; k++)
					{
						array[iLength++] = array2[num4++];
					}
					PrintTransmit(array, iLength);
					Thread.Sleep(100);
				}
			}
			MessageBox.Show("Download font library complete!", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message + "\r\nPlease select a font library file!", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void btSpecialStatus_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) == 0)
			{
				m_iSpecialStatus = GetStatusspecial();
				if (m_iSpecialStatus == 1)
				{
					m_iSpecialStatus = GetStatusspecial();
				}
				switch (m_iSpecialStatus)
				{
				case 0:
					tb_SpecialStatus.Text = m_iSpecialStatus + " - " + GetStringRes("R10014", "Printer is ready");
					break;
				case 1:
					tb_SpecialStatus.Text = m_iSpecialStatus + " - " + GetStringRes("R10015", "Printer is offline or no power");
					break;
				case 2:
					tb_SpecialStatus.Text = m_iSpecialStatus + " - " + GetStringRes("R10016", "Printer called unmatched library");
					break;
				case 3:
					tb_SpecialStatus.Text = m_iSpecialStatus + " - " + GetStringRes("R10026", "Current printer can't support special function");
					break;
				case 4:
					tb_SpecialStatus.Text = m_iSpecialStatus + " - " + GetStringRes("R10027", "Printer doesn't load paper to presenter");
					break;
				case 5:
					tb_SpecialStatus.Text = m_iSpecialStatus + " - " + GetStringRes("R10028", "Paper is blocked in printer bezel");
					break;
				case 6:
					tb_SpecialStatus.Text = m_iSpecialStatus + " - " + GetStringRes("R10029", "Paper jams in printer mechanism");
					break;
				case 7:
					tb_SpecialStatus.Text = m_iSpecialStatus + " - " + GetStringRes("R10030", "Unfinished ticket is dragged by outside force");
					break;
				case 8:
					tb_SpecialStatus.Text = m_iSpecialStatus + " - " + GetStringRes("R10031", "There is ticket held on printer beze");
					break;
				}
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void btPaperDetection_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) == 0)
			{
				byte[] array = new byte[3] { 27, 204, 1 };
				byte[] array2 = new byte[16];
				int transmit = GetTransmit(array, array.Length, array2, array2.Length);
				int num = -1;
				if (transmit > 0)
				{
					num = array2[0];
				}
				if (num == -1)
				{
					tb_PaperDetection.Text = num + " - " + GetStringRes("R10034", "Failure");
				}
				else if (num < 40 || num > 230)
				{
					tb_PaperDetection.Text = num + " - " + GetStringRes("R10032", "Normal");
				}
				else
				{
					tb_PaperDetection.Text = num + " - " + GetStringRes("R10033", "Abnormal");
				}
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void btnSetBlackAD_Click(object sender, EventArgs e)
	{
		try
		{
			int num = Convert.ToInt32(tb_BlackAD.Text.Trim());
			if (num < 32 || num > 200)
			{
				MessageBox.Show("Invalid parameter!", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				tb_BlackAD.Focus();
			}
			else if (FunOpenPort(1) == 0)
			{
				byte[] array = new byte[5]
				{
					19,
					116,
					17,
					102,
					(byte)num
				};
				PrintTransmit(array, array.Length);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void btnBlackTest_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) == 0)
			{
				StringBuilder strData = new StringBuilder("Black markposition test!");
				PrintString(strData);
				Thread.Sleep(10);
				byte[] array = new byte[9] { 10, 10, 27, 74, 100, 29, 12, 27, 109 };
				PrintTransmit(array, array.Length);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void btnSpeedStart_Click(object sender, EventArgs e)
	{
		try
		{
			if (FunOpenPort(1) != 0)
			{
				return;
			}
			Thread.Sleep(1000);
			m_iStatus = GetStatus();
			if (m_iStatus != 0)
			{
				MessageBox.Show(GetStringRes("", "Printer not ready"), "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			int iCount = int.Parse(txtSpeedCount.Text);
			Thread thread = new Thread((ThreadStart)delegate
			{
				for (int i = 1; i <= iCount; i++)
				{
					m_sbData = new StringBuilder(i + ":" + txtSpeedData.Text);
					PrintString(m_sbData, 0);
				}
				int num = 0;
				byte[] array = new byte[3] { 29, 114, 1 };
				byte[] array2 = new byte[64];
				for (int i = 1; i <= iCount; i++)
				{
					num = GetTransmit(array, array.Length, array2, array2.Length);
					if (num > 0)
					{
						break;
					}
					Thread.Sleep(100);
				}
				m_frmSpeedBox.UpdateRecvText2(iCount, "");
			});
			thread.Start();
			m_frmSpeedBox.ShowDialog();
			thread.Abort();
			try
			{
				Settings.Default.SpeedData = txtSpeedData.Text;
				Settings.Default.SpeedCount = txtSpeedCount.Text;
				Settings.Default.Save();
			}
			catch (Exception)
			{
			}
		}
		catch (Exception ex2)
		{
			MessageBox.Show(ex2.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void cboDC01_SelectedIndexChanged(object sender, EventArgs e)
	{
		try
		{
			ComboBox comboBox = (ComboBox)sender;
			m_iDC01SelectIndex = cboDC01.SelectedIndex + 1;
			FillDCExample(m_iDC01SelectIndex);
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	private void FillDCExample(int iExample_ID)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected O, but got Unknown
		m_dsRS_DC = new DataSet();
		m_str_Sql = $"select F_Cmd_ID,F_Select,F_Hex,F_Color,F_Content,F_Comment,F_Sleep,F_Seq from T_DCInfo  Where F_Example_ID = {iExample_ID} order by F_Seq";
		((DbCommand)(object)m_cmd).CommandText = m_str_Sql;
		m_sda_Comm = new SQLiteDataAdapter(m_cmd);
		((DataAdapter)(object)m_sda_Comm).Fill(m_dsRS_DC);
		int count = m_dsRS_DC.Tables[0].Rows.Count;
		for (int i = count; i < 100; i++)
		{
			m_dsRS_DC.Tables[0].Rows.Add(i);
			m_dsRS_DC.Tables[0].Rows[i]["F_Cmd_ID"] = i + 1;
			m_dsRS_DC.Tables[0].Rows[i]["F_Select"] = 0;
			m_dsRS_DC.Tables[0].Rows[i]["F_Hex"] = 0;
			m_dsRS_DC.Tables[0].Rows[i]["F_Color"] = 0;
			m_dsRS_DC.Tables[0].Rows[i]["F_Sleep"] = 10;
			m_dsRS_DC.Tables[0].Rows[i]["F_Seq"] = (i + 1) * 10;
		}
		dgvDC01.DataSource = m_dsRS_DC.Tables[0];
	}

	private bool DCDataCheck()
	{
		bool result = false;
		try
		{
			m_dv = new DataView();
			m_dv.Table = m_dsRS_DC.Tables[0];
			m_dv.RowFilter = "F_Select = true ";
			int count = m_dv.Count;
			if (count == 0)
			{
				MessageBox.Show("No command selected!", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return result;
			}
			int num = 0;
			string text = "";
			string text2 = "";
			int num2 = 0;
			for (num = 0; num < count; num++)
			{
				text = m_dv[num]["F_Content"].ToString();
				num2 = int.Parse(m_dv[num]["F_Cmd_ID"].ToString()) - 1;
				if (text.Equals(""))
				{
					MessageBox.Show("Content is empty", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					dgvDC01.CurrentCell = dgvDC01.Rows[num2].Cells["F_Content"];
					return result;
				}
				text2 = m_dv[num]["F_Hex"].ToString();
				if (text2.Equals("1"))
				{
					try
					{
						byte[] array = Utility.StrToHexByte(text);
					}
					catch (Exception)
					{
						MessageBox.Show(GetStringRes("R10003", "The input character format is wrong!"), "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						dgvDC01.CurrentCell = dgvDC01.Rows[num2].Cells["F_DCContent"];
						return result;
					}
				}
			}
			result = true;
		}
		catch (Exception ex2)
		{
			MessageBox.Show("Data check failure!\n" + ex2.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		return result;
	}

	private bool DCDataSave()
	{
		bool result = false;
		int num = 0;
		SQLiteTransaction val = m_cnn.BeginTransaction();
		try
		{
			int iDC01SelectIndex = m_iDC01SelectIndex;
			string text = cboDC01.Text.Trim();
			m_cmd.Transaction = val;
			m_str_Sql = "delete from T_DCInfo Where F_Example_ID = " + iDC01SelectIndex;
			((DbCommand)(object)m_cmd).CommandText = m_str_Sql;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			((DbCommand)(object)m_cmd).ExecuteNonQuery();
			int count = m_dsRS_DC.Tables[0].Rows.Count;
			for (num = 0; num < count; num++)
			{
				num4 = (m_dsRS_DC.Tables[0].Rows[num]["F_Select"].ToString().Equals("1") ? 1 : 0);
				num2 = (m_dsRS_DC.Tables[0].Rows[num]["F_Hex"].ToString().Equals("1") ? 1 : 0);
				num3 = (m_dsRS_DC.Tables[0].Rows[num]["F_Color"].ToString().Equals("1") ? 1 : 0);
				if (m_dsRS_DC.Tables[0].Rows[num]["F_Seq"].ToString().Trim().Equals(""))
				{
					num5 += 10;
					m_dsRS_DC.Tables[0].Rows[num]["F_Seq"] = num5;
				}
				else
				{
					num5 = int.Parse(m_dsRS_DC.Tables[0].Rows[num]["F_Seq"].ToString());
				}
				if (m_dsRS_DC.Tables[0].Rows[num]["F_Sleep"].ToString().Trim().Equals(""))
				{
					m_dsRS_DC.Tables[0].Rows[num]["F_Sleep"] = "10";
				}
				m_str_Sql = string.Format("Insert into T_DCInfo (F_Cmd_ID,F_Select,F_Hex,F_Content,F_Comment,F_Seq,F_Sleep,F_Example_ID,F_Example_Name,F_Color)  values ({0},{1},{2},'{3}','{4}',{5},{6},{7},'{8}',{9})", num + 1, num4, num2, m_dsRS_DC.Tables[0].Rows[num]["F_Content"], m_dsRS_DC.Tables[0].Rows[num]["F_Comment"], m_dsRS_DC.Tables[0].Rows[num]["F_Seq"], m_dsRS_DC.Tables[0].Rows[num]["F_Sleep"], iDC01SelectIndex, text, num3);
				((DbCommand)(object)m_cmd).CommandText = m_str_Sql;
				((DbCommand)(object)m_cmd).ExecuteNonQuery();
			}
			((DbTransaction)(object)val).Commit();
			cboDC01.Items.Insert(iDC01SelectIndex - 1, text);
			cboDC01.SelectedIndex = iDC01SelectIndex - 1;
			cboDC01.Items.RemoveAt(iDC01SelectIndex);
			result = true;
		}
		catch (Exception ex)
		{
			((DbTransaction)(object)val).Rollback();
			MessageBox.Show(GetStringRes("R10012", "Data save failure!\n") + ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		return result;
	}

	private void btnDCPrint_Click(object sender, EventArgs e)
	{
		try
		{
			if (!DCDataCheck() || !DCDataSave() || FunOpenPort(1) != 0)
			{
				return;
			}
			Cursor = Cursors.WaitCursor;
			int count = m_dv.Count;
			m_dv.Sort = "F_Seq,F_Cmd_ID";
			int num = 0;
			string text = "";
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			byte[] array = new byte[3] { 27, 114, 0 };
			for (num = 0; num < count; num++)
			{
				text = m_dv[num]["F_Hex"].ToString();
				try
				{
					num3 = ((!m_dv[num]["F_Hex"].ToString().Equals("")) ? short.Parse(m_dv[num]["F_Hex"].ToString()) : 0);
				}
				catch (Exception)
				{
					num3 = 0;
				}
				text = m_dv[num]["F_Color"].ToString();
				try
				{
					num4 = ((!m_dv[num]["F_Color"].ToString().Equals("")) ? short.Parse(m_dv[num]["F_Color"].ToString()) : 0);
				}
				catch (Exception)
				{
					num4 = 0;
				}
				array[2] = (byte)num4;
				text = m_dv[num]["F_Content"].ToString();
				PrintTransmit(array, 3);
				if (num3 == 0)
				{
					PrintString(new StringBuilder(text));
				}
				else
				{
					byte[] array2 = Utility.StrToHexByte(text);
					PrintTransmit(array2, array2.Length);
				}
				text = m_dv[num]["F_Sleep"].ToString();
				num2 = int.Parse(m_dv[num]["F_Sleep"].ToString());
				Thread.Sleep(num2);
			}
		}
		catch (Exception ex3)
		{
			MessageBox.Show(ex3.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		Cursor = Cursors.Default;
	}

	private void dgvDC01_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
	{
		int num = 0;
		int num2 = 0;
		try
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "IMG file|*.bmp;*.jpg;*.png|BMP file|*.bmp|JPG file|*.jpg;*.jpeg|PNG file|*.png";
			openFileDialog.RestoreDirectory = true;
			openFileDialog.FilterIndex = 1;
			if (openFileDialog.ShowDialog() != DialogResult.OK)
			{
				return;
			}
			string fileName = openFileDialog.FileName;
			if (fileName == null)
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder();
			int num3 = 0;
			byte[] array = new byte[4194304];
			num3 = GetBMPBufferDCDATA(new StringBuilder(fileName), array, 4194304);
			for (num = 0; num < num3; num++)
			{
				num2 = (array[num] + 256) % 256;
				if (num2 < 16)
				{
					stringBuilder.Append("0" + $"{num2,1:X}" + " ");
				}
				else
				{
					stringBuilder.Append($"{num2,2:X}" + " ");
				}
			}
			dgvDC01.Rows[e.RowIndex].Cells["F_DCContent"].Value = stringBuilder.ToString();
			dgvDC01.Rows[e.RowIndex].Cells["F_DCHex"].Value = true;
			if (stringBuilder.ToString().Length > 24)
			{
				dgvDC01.Rows[e.RowIndex].Cells["F_DCComment"].Value = stringBuilder.ToString().Substring(0, 24) + "...";
			}
			dgvDC01.Update();
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MSPrinterTools.FrmMain));
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle13 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle14 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle15 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle16 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle17 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle18 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle19 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle20 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle21 = new System.Windows.Forms.DataGridViewCellStyle();
		System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle22 = new System.Windows.Forms.DataGridViewCellStyle();
		this.groupBox1 = new System.Windows.Forms.GroupBox();
		this.cboPrintConnValue2 = new System.Windows.Forms.ComboBox();
		this.lblPrintConnValue2 = new System.Windows.Forms.Label();
		this.btn_SetInit = new System.Windows.Forms.Button();
		this.cboBandrate = new System.Windows.Forms.ComboBox();
		this.lblPrintConnValue1 = new System.Windows.Forms.Label();
		this.cboPort = new System.Windows.Forms.ComboBox();
		this.label2 = new System.Windows.Forms.Label();
		this.btn_SelfCheck = new System.Windows.Forms.Button();
		this.tabControl1 = new System.Windows.Forms.TabControl();
		this.tabPage1 = new System.Windows.Forms.TabPage();
		this.gb_Receive = new System.Windows.Forms.GroupBox();
		this.bt_ClearReceiveContent = new System.Windows.Forms.Button();
		this.tb_ReceiveContent = new System.Windows.Forms.TextBox();
		this.gb_Send = new System.Windows.Forms.GroupBox();
		this.lblCodePage = new System.Windows.Forms.Label();
		this.cboCodePage = new System.Windows.Forms.ComboBox();
		this.button4 = new System.Windows.Forms.Button();
		this.bCutPaper = new System.Windows.Forms.CheckBox();
		this.bt_SendToPrinter = new System.Windows.Forms.Button();
		this.tb_SendContentT = new System.Windows.Forms.TextBox();
		this.bt_ClearSendContent = new System.Windows.Forms.Button();
		this.rdb_SendTypeHEX = new System.Windows.Forms.RadioButton();
		this.rdb_SendTypeText = new System.Windows.Forms.RadioButton();
		this.tb_SendContentH = new System.Windows.Forms.TextBox();
		this.gb_BasicTest = new System.Windows.Forms.GroupBox();
		this.label46 = new System.Windows.Forms.Label();
		this.btPaperDetection = new System.Windows.Forms.Button();
		this.tb_PaperDetection = new System.Windows.Forms.TextBox();
		this.label44 = new System.Windows.Forms.Label();
		this.btSpecialStatus = new System.Windows.Forms.Button();
		this.tb_SpecialStatus = new System.Windows.Forms.TextBox();
		this.chkSDKFunAll = new System.Windows.Forms.CheckBox();
		this.label10 = new System.Windows.Forms.Label();
		this.btnExample = new System.Windows.Forms.Button();
		this.cboExample = new System.Windows.Forms.ComboBox();
		this.label1 = new System.Windows.Forms.Label();
		this.btnPrint1 = new System.Windows.Forms.Button();
		this.cboSDKFunction = new System.Windows.Forms.ComboBox();
		this.label6 = new System.Windows.Forms.Label();
		this.tb_ProductMessage = new System.Windows.Forms.TextBox();
		this.bt_PrintBMP = new System.Windows.Forms.Button();
		this.label45 = new System.Windows.Forms.Label();
		this.bt_GetProductMessage = new System.Windows.Forms.Button();
		this.bt_GetStatus = new System.Windows.Forms.Button();
		this.tb_Status = new System.Windows.Forms.TextBox();
		this.tb_bmpFilePath = new System.Windows.Forms.TextBox();
		this.label5 = new System.Windows.Forms.Label();
		this.tabPage2 = new System.Windows.Forms.TabPage();
		this.chkPrintIndex = new System.Windows.Forms.CheckBox();
		this.label4 = new System.Windows.Forms.Label();
		this.cboMulti2 = new System.Windows.Forms.ComboBox();
		this.label27 = new System.Windows.Forms.Label();
		this.cboMulti1 = new System.Windows.Forms.ComboBox();
		this.btnMulti1 = new System.Windows.Forms.Button();
		this.groupBox7 = new System.Windows.Forms.GroupBox();
		this.dgvRS = new System.Windows.Forms.DataGridView();
		this.F_Cmd_ID = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.F_Select = new System.Windows.Forms.DataGridViewCheckBoxColumn();
		this.F_Hex = new System.Windows.Forms.DataGridViewCheckBoxColumn();
		this.F_Content = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.F_Comment = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.F_Seq = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.F_Sleep = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabPage3 = new System.Windows.Forms.TabPage();
		this.groupBox5 = new System.Windows.Forms.GroupBox();
		this.groupBox12 = new System.Windows.Forms.GroupBox();
		this.txtFontPath = new System.Windows.Forms.TextBox();
		this.btnFontDownload = new System.Windows.Forms.Button();
		this.label43 = new System.Windows.Forms.Label();
		this.groupBox6 = new System.Windows.Forms.GroupBox();
		this.chkFontEpson = new System.Windows.Forms.CheckBox();
		this.label25 = new System.Windows.Forms.Label();
		this.comboBox10 = new System.Windows.Forms.ComboBox();
		this.button1 = new System.Windows.Forms.Button();
		this.groupBox4 = new System.Windows.Forms.GroupBox();
		this.chkFont2017All = new System.Windows.Forms.CheckBox();
		this.btnFont2017All = new System.Windows.Forms.Button();
		this.label14 = new System.Windows.Forms.Label();
		this.comboBox8 = new System.Windows.Forms.ComboBox();
		this.button2 = new System.Windows.Forms.Button();
		this.button3 = new System.Windows.Forms.Button();
		this.groupBox3 = new System.Windows.Forms.GroupBox();
		this.btnBlackTest = new System.Windows.Forms.Button();
		this.label47 = new System.Windows.Forms.Label();
		this.tb_BlackAD = new System.Windows.Forms.TextBox();
		this.label48 = new System.Windows.Forms.Label();
		this.btnSetBlackAD = new System.Windows.Forms.Button();
		this.textBox1 = new System.Windows.Forms.TextBox();
		this.label42 = new System.Windows.Forms.Label();
		this.button7 = new System.Windows.Forms.Button();
		this.button6 = new System.Windows.Forms.Button();
		this.comboBox2 = new System.Windows.Forms.ComboBox();
		this.label23 = new System.Windows.Forms.Label();
		this.label28 = new System.Windows.Forms.Label();
		this.textBox6 = new System.Windows.Forms.TextBox();
		this.label12 = new System.Windows.Forms.Label();
		this.label24 = new System.Windows.Forms.Label();
		this.label13 = new System.Windows.Forms.Label();
		this.button22 = new System.Windows.Forms.Button();
		this.comboBox5 = new System.Windows.Forms.ComboBox();
		this.button21 = new System.Windows.Forms.Button();
		this.comboBox3 = new System.Windows.Forms.ComboBox();
		this.comboBox18 = new System.Windows.Forms.ComboBox();
		this.comboBox4 = new System.Windows.Forms.ComboBox();
		this.label21 = new System.Windows.Forms.Label();
		this.button37 = new System.Windows.Forms.Button();
		this.label20 = new System.Windows.Forms.Label();
		this.button30 = new System.Windows.Forms.Button();
		this.label19 = new System.Windows.Forms.Label();
		this.button31 = new System.Windows.Forms.Button();
		this.button33 = new System.Windows.Forms.Button();
		this.textBox13 = new System.Windows.Forms.TextBox();
		this.label18 = new System.Windows.Forms.Label();
		this.button20 = new System.Windows.Forms.Button();
		this.label15 = new System.Windows.Forms.Label();
		this.comboBox7 = new System.Windows.Forms.ComboBox();
		this.button18 = new System.Windows.Forms.Button();
		this.label17 = new System.Windows.Forms.Label();
		this.label16 = new System.Windows.Forms.Label();
		this.btnSetting6 = new System.Windows.Forms.Button();
		this.cboSetting6 = new System.Windows.Forms.ComboBox();
		this.groupBox2 = new System.Windows.Forms.GroupBox();
		this.cboFontLib = new System.Windows.Forms.ComboBox();
		this.cb_Character = new System.Windows.Forms.ComboBox();
		this.chkFontB = new System.Windows.Forms.CheckBox();
		this.button5 = new System.Windows.Forms.Button();
		this.label22 = new System.Windows.Forms.Label();
		this.comboBox9 = new System.Windows.Forms.ComboBox();
		this.label9 = new System.Windows.Forms.Label();
		this.tabPage4 = new System.Windows.Forms.TabPage();
		this.btnSetNV = new System.Windows.Forms.Button();
		this.label26 = new System.Windows.Forms.Label();
		this.cboNVIndex = new System.Windows.Forms.ComboBox();
		this.btnNVPrint = new System.Windows.Forms.Button();
		this.groupBox8 = new System.Windows.Forms.GroupBox();
		this.dgvNV = new System.Windows.Forms.DataGridView();
		this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.F_FilePath = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.tabPage5 = new System.Windows.Forms.TabPage();
		this.groupBox11 = new System.Windows.Forms.GroupBox();
		this.btnWifiRead = new System.Windows.Forms.Button();
		this.btnWifiDNS = new System.Windows.Forms.Button();
		this.btnWifiGateway = new System.Windows.Forms.Button();
		this.btnWifiMask = new System.Windows.Forms.Button();
		this.btnWifiIPAddr = new System.Windows.Forms.Button();
		this.txtWifiDNS = new System.Windows.Forms.TextBox();
		this.txtWifiGateway = new System.Windows.Forms.TextBox();
		this.txtWifiMask = new System.Windows.Forms.TextBox();
		this.txtWifiIPAddr = new System.Windows.Forms.TextBox();
		this.label41 = new System.Windows.Forms.Label();
		this.label40 = new System.Windows.Forms.Label();
		this.label39 = new System.Windows.Forms.Label();
		this.label38 = new System.Windows.Forms.Label();
		this.groupBox10 = new System.Windows.Forms.GroupBox();
		this.txtWifiSubscribe3 = new System.Windows.Forms.TextBox();
		this.label54 = new System.Windows.Forms.Label();
		this.txtWifiSubscribe2 = new System.Windows.Forms.TextBox();
		this.label53 = new System.Windows.Forms.Label();
		this.txtWifiPublish3 = new System.Windows.Forms.TextBox();
		this.label52 = new System.Windows.Forms.Label();
		this.txtWifiPublish2 = new System.Windows.Forms.TextBox();
		this.label49 = new System.Windows.Forms.Label();
		this.txtWifiMQTTPassword = new System.Windows.Forms.TextBox();
		this.btnWifiSet1 = new System.Windows.Forms.Button();
		this.btnWifiSet6 = new System.Windows.Forms.Button();
		this.label7 = new System.Windows.Forms.Label();
		this.txtWifiSubscribe1 = new System.Windows.Forms.TextBox();
		this.txtWifiIP = new System.Windows.Forms.TextBox();
		this.label36 = new System.Windows.Forms.Label();
		this.label8 = new System.Windows.Forms.Label();
		this.txtWifiPublish1 = new System.Windows.Forms.TextBox();
		this.txtWifiPort = new System.Windows.Forms.TextBox();
		this.label37 = new System.Windows.Forms.Label();
		this.label30 = new System.Windows.Forms.Label();
		this.txtWifiMQTTClientName = new System.Windows.Forms.TextBox();
		this.cboWIFIModel = new System.Windows.Forms.ComboBox();
		this.label35 = new System.Windows.Forms.Label();
		this.btnWifiSet3 = new System.Windows.Forms.Button();
		this.btnWifiSet5 = new System.Windows.Forms.Button();
		this.label32 = new System.Windows.Forms.Label();
		this.txtWifiMQTTPort = new System.Windows.Forms.TextBox();
		this.txtWifiMQTTClientID = new System.Windows.Forms.TextBox();
		this.label33 = new System.Windows.Forms.Label();
		this.label31 = new System.Windows.Forms.Label();
		this.txtWifiMQTTIP = new System.Windows.Forms.TextBox();
		this.btnWifiSet4 = new System.Windows.Forms.Button();
		this.label34 = new System.Windows.Forms.Label();
		this.groupBox9 = new System.Windows.Forms.GroupBox();
		this.txtWifiWan = new System.Windows.Forms.TextBox();
		this.label29 = new System.Windows.Forms.Label();
		this.btnWifiSet2 = new System.Windows.Forms.Button();
		this.label11 = new System.Windows.Forms.Label();
		this.txtWifiPassword = new System.Windows.Forms.TextBox();
		this.tabPage6 = new System.Windows.Forms.TabPage();
		this.groupBox13 = new System.Windows.Forms.GroupBox();
		this.txtSpeedCount = new System.Windows.Forms.TextBox();
		this.txtSpeedData = new System.Windows.Forms.TextBox();
		this.label51 = new System.Windows.Forms.Label();
		this.label50 = new System.Windows.Forms.Label();
		this.btnSpeedStart = new System.Windows.Forms.Button();
		this.tabPage7 = new System.Windows.Forms.TabPage();
		this.cboDC01 = new System.Windows.Forms.ComboBox();
		this.lblDC01 = new System.Windows.Forms.Label();
		this.btnDCPrint = new System.Windows.Forms.Button();
		this.groupBox14 = new System.Windows.Forms.GroupBox();
		this.dgvDC01 = new System.Windows.Forms.DataGridView();
		this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dataGridViewCheckBoxColumn1 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
		this.F_DCHex = new System.Windows.Forms.DataGridViewCheckBoxColumn();
		this.Color = new System.Windows.Forms.DataGridViewCheckBoxColumn();
		this.F_DCContent = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.F_DCComment = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.dataGridViewTextBoxColumn8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
		this.languaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.englishToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
		this.menuStrip1 = new System.Windows.Forms.MenuStrip();
		this.groupBox1.SuspendLayout();
		this.tabControl1.SuspendLayout();
		this.tabPage1.SuspendLayout();
		this.gb_Receive.SuspendLayout();
		this.gb_Send.SuspendLayout();
		this.gb_BasicTest.SuspendLayout();
		this.tabPage2.SuspendLayout();
		this.groupBox7.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dgvRS).BeginInit();
		this.tabPage3.SuspendLayout();
		this.groupBox5.SuspendLayout();
		this.groupBox12.SuspendLayout();
		this.groupBox6.SuspendLayout();
		this.groupBox4.SuspendLayout();
		this.groupBox3.SuspendLayout();
		this.groupBox2.SuspendLayout();
		this.tabPage4.SuspendLayout();
		this.groupBox8.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dgvNV).BeginInit();
		this.tabPage5.SuspendLayout();
		this.groupBox11.SuspendLayout();
		this.groupBox10.SuspendLayout();
		this.groupBox9.SuspendLayout();
		this.tabPage6.SuspendLayout();
		this.groupBox13.SuspendLayout();
		this.tabPage7.SuspendLayout();
		this.groupBox14.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this.dgvDC01).BeginInit();
		this.menuStrip1.SuspendLayout();
		base.SuspendLayout();
		this.groupBox1.Controls.Add(this.cboPrintConnValue2);
		this.groupBox1.Controls.Add(this.lblPrintConnValue2);
		this.groupBox1.Controls.Add(this.btn_SetInit);
		this.groupBox1.Controls.Add(this.cboBandrate);
		this.groupBox1.Controls.Add(this.lblPrintConnValue1);
		this.groupBox1.Controls.Add(this.cboPort);
		this.groupBox1.Controls.Add(this.label2);
		this.groupBox1.Controls.Add(this.btn_SelfCheck);
		resources.ApplyResources(this.groupBox1, "groupBox1");
		this.groupBox1.Name = "groupBox1";
		this.groupBox1.TabStop = false;
		resources.ApplyResources(this.cboPrintConnValue2, "cboPrintConnValue2");
		this.cboPrintConnValue2.FormattingEnabled = true;
		this.cboPrintConnValue2.Name = "cboPrintConnValue2";
		resources.ApplyResources(this.lblPrintConnValue2, "lblPrintConnValue2");
		this.lblPrintConnValue2.Name = "lblPrintConnValue2";
		resources.ApplyResources(this.btn_SetInit, "btn_SetInit");
		this.btn_SetInit.Name = "btn_SetInit";
		this.btn_SetInit.UseVisualStyleBackColor = true;
		this.btn_SetInit.Click += new System.EventHandler(btn_SetInit_Click);
		this.cboBandrate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		resources.ApplyResources(this.cboBandrate, "cboBandrate");
		this.cboBandrate.FormattingEnabled = true;
		this.cboBandrate.Name = "cboBandrate";
		resources.ApplyResources(this.lblPrintConnValue1, "lblPrintConnValue1");
		this.lblPrintConnValue1.Name = "lblPrintConnValue1";
		this.cboPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		resources.ApplyResources(this.cboPort, "cboPort");
		this.cboPort.FormattingEnabled = true;
		this.cboPort.Name = "cboPort";
		this.cboPort.Sorted = true;
		this.cboPort.SelectedIndexChanged += new System.EventHandler(cboPort_SelectedIndexChanged);
		resources.ApplyResources(this.label2, "label2");
		this.label2.Name = "label2";
		resources.ApplyResources(this.btn_SelfCheck, "btn_SelfCheck");
		this.btn_SelfCheck.Name = "btn_SelfCheck";
		this.btn_SelfCheck.UseVisualStyleBackColor = true;
		this.btn_SelfCheck.Click += new System.EventHandler(btn_SelfCheck_Click);
		this.tabControl1.Controls.Add(this.tabPage1);
		this.tabControl1.Controls.Add(this.tabPage2);
		this.tabControl1.Controls.Add(this.tabPage3);
		this.tabControl1.Controls.Add(this.tabPage4);
		this.tabControl1.Controls.Add(this.tabPage5);
		this.tabControl1.Controls.Add(this.tabPage6);
		this.tabControl1.Controls.Add(this.tabPage7);
		resources.ApplyResources(this.tabControl1, "tabControl1");
		this.tabControl1.Name = "tabControl1";
		this.tabControl1.SelectedIndex = 0;
		this.tabControl1.SelectedIndexChanged += new System.EventHandler(tabControl1_SelectedIndexChanged);
		this.tabPage1.BackColor = System.Drawing.SystemColors.ButtonFace;
		this.tabPage1.Controls.Add(this.gb_Receive);
		this.tabPage1.Controls.Add(this.gb_Send);
		this.tabPage1.Controls.Add(this.gb_BasicTest);
		resources.ApplyResources(this.tabPage1, "tabPage1");
		this.tabPage1.Name = "tabPage1";
		this.gb_Receive.Controls.Add(this.bt_ClearReceiveContent);
		this.gb_Receive.Controls.Add(this.tb_ReceiveContent);
		resources.ApplyResources(this.gb_Receive, "gb_Receive");
		this.gb_Receive.Name = "gb_Receive";
		this.gb_Receive.TabStop = false;
		resources.ApplyResources(this.bt_ClearReceiveContent, "bt_ClearReceiveContent");
		this.bt_ClearReceiveContent.Name = "bt_ClearReceiveContent";
		this.bt_ClearReceiveContent.UseVisualStyleBackColor = true;
		this.bt_ClearReceiveContent.Click += new System.EventHandler(bt_ClearReceiveContent_Click_1);
		this.tb_ReceiveContent.BackColor = System.Drawing.Color.LightGray;
		resources.ApplyResources(this.tb_ReceiveContent, "tb_ReceiveContent");
		this.tb_ReceiveContent.Name = "tb_ReceiveContent";
		this.tb_ReceiveContent.ReadOnly = true;
		this.gb_Send.Controls.Add(this.lblCodePage);
		this.gb_Send.Controls.Add(this.cboCodePage);
		this.gb_Send.Controls.Add(this.button4);
		this.gb_Send.Controls.Add(this.bCutPaper);
		this.gb_Send.Controls.Add(this.bt_SendToPrinter);
		this.gb_Send.Controls.Add(this.tb_SendContentT);
		this.gb_Send.Controls.Add(this.bt_ClearSendContent);
		this.gb_Send.Controls.Add(this.rdb_SendTypeHEX);
		this.gb_Send.Controls.Add(this.rdb_SendTypeText);
		this.gb_Send.Controls.Add(this.tb_SendContentH);
		resources.ApplyResources(this.gb_Send, "gb_Send");
		this.gb_Send.Name = "gb_Send";
		this.gb_Send.TabStop = false;
		resources.ApplyResources(this.lblCodePage, "lblCodePage");
		this.lblCodePage.Name = "lblCodePage";
		this.cboCodePage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cboCodePage.FormattingEnabled = true;
		resources.ApplyResources(this.cboCodePage, "cboCodePage");
		this.cboCodePage.Name = "cboCodePage";
		this.cboCodePage.SelectedIndexChanged += new System.EventHandler(cboCodePage_SelectedIndexChanged);
		resources.ApplyResources(this.button4, "button4");
		this.button4.Name = "button4";
		this.button4.UseVisualStyleBackColor = true;
		this.button4.Click += new System.EventHandler(button4_Click);
		resources.ApplyResources(this.bCutPaper, "bCutPaper");
		this.bCutPaper.Name = "bCutPaper";
		this.bCutPaper.UseVisualStyleBackColor = true;
		resources.ApplyResources(this.bt_SendToPrinter, "bt_SendToPrinter");
		this.bt_SendToPrinter.Name = "bt_SendToPrinter";
		this.bt_SendToPrinter.UseVisualStyleBackColor = true;
		this.bt_SendToPrinter.Click += new System.EventHandler(bt_SendToPrinter_Click);
		resources.ApplyResources(this.tb_SendContentT, "tb_SendContentT");
		this.tb_SendContentT.Name = "tb_SendContentT";
		resources.ApplyResources(this.bt_ClearSendContent, "bt_ClearSendContent");
		this.bt_ClearSendContent.Name = "bt_ClearSendContent";
		this.bt_ClearSendContent.UseVisualStyleBackColor = true;
		this.bt_ClearSendContent.Click += new System.EventHandler(bt_ClearSendContent_Click);
		resources.ApplyResources(this.rdb_SendTypeHEX, "rdb_SendTypeHEX");
		this.rdb_SendTypeHEX.Checked = true;
		this.rdb_SendTypeHEX.Name = "rdb_SendTypeHEX";
		this.rdb_SendTypeHEX.TabStop = true;
		this.rdb_SendTypeHEX.UseVisualStyleBackColor = true;
		this.rdb_SendTypeHEX.CheckedChanged += new System.EventHandler(rdb_SendTypeHEX_CheckedChanged);
		resources.ApplyResources(this.rdb_SendTypeText, "rdb_SendTypeText");
		this.rdb_SendTypeText.Name = "rdb_SendTypeText";
		this.rdb_SendTypeText.UseVisualStyleBackColor = true;
		this.rdb_SendTypeText.CheckedChanged += new System.EventHandler(rdb_SendTypeText_CheckedChanged);
		resources.ApplyResources(this.tb_SendContentH, "tb_SendContentH");
		this.tb_SendContentH.Name = "tb_SendContentH";
		this.gb_BasicTest.BackColor = System.Drawing.SystemColors.ButtonFace;
		this.gb_BasicTest.Controls.Add(this.label46);
		this.gb_BasicTest.Controls.Add(this.btPaperDetection);
		this.gb_BasicTest.Controls.Add(this.tb_PaperDetection);
		this.gb_BasicTest.Controls.Add(this.label44);
		this.gb_BasicTest.Controls.Add(this.btSpecialStatus);
		this.gb_BasicTest.Controls.Add(this.tb_SpecialStatus);
		this.gb_BasicTest.Controls.Add(this.chkSDKFunAll);
		this.gb_BasicTest.Controls.Add(this.label10);
		this.gb_BasicTest.Controls.Add(this.btnExample);
		this.gb_BasicTest.Controls.Add(this.cboExample);
		this.gb_BasicTest.Controls.Add(this.label1);
		this.gb_BasicTest.Controls.Add(this.btnPrint1);
		this.gb_BasicTest.Controls.Add(this.cboSDKFunction);
		this.gb_BasicTest.Controls.Add(this.label6);
		this.gb_BasicTest.Controls.Add(this.tb_ProductMessage);
		this.gb_BasicTest.Controls.Add(this.bt_PrintBMP);
		this.gb_BasicTest.Controls.Add(this.label45);
		this.gb_BasicTest.Controls.Add(this.bt_GetProductMessage);
		this.gb_BasicTest.Controls.Add(this.bt_GetStatus);
		this.gb_BasicTest.Controls.Add(this.tb_Status);
		this.gb_BasicTest.Controls.Add(this.tb_bmpFilePath);
		this.gb_BasicTest.Controls.Add(this.label5);
		resources.ApplyResources(this.gb_BasicTest, "gb_BasicTest");
		this.gb_BasicTest.Name = "gb_BasicTest";
		this.gb_BasicTest.TabStop = false;
		resources.ApplyResources(this.label46, "label46");
		this.label46.Name = "label46";
		resources.ApplyResources(this.btPaperDetection, "btPaperDetection");
		this.btPaperDetection.Name = "btPaperDetection";
		this.btPaperDetection.UseVisualStyleBackColor = true;
		this.btPaperDetection.Click += new System.EventHandler(btPaperDetection_Click);
		resources.ApplyResources(this.tb_PaperDetection, "tb_PaperDetection");
		this.tb_PaperDetection.Name = "tb_PaperDetection";
		this.tb_PaperDetection.ReadOnly = true;
		resources.ApplyResources(this.label44, "label44");
		this.label44.Name = "label44";
		resources.ApplyResources(this.btSpecialStatus, "btSpecialStatus");
		this.btSpecialStatus.Name = "btSpecialStatus";
		this.btSpecialStatus.UseVisualStyleBackColor = true;
		this.btSpecialStatus.Click += new System.EventHandler(btSpecialStatus_Click);
		resources.ApplyResources(this.tb_SpecialStatus, "tb_SpecialStatus");
		this.tb_SpecialStatus.Name = "tb_SpecialStatus";
		this.tb_SpecialStatus.ReadOnly = true;
		resources.ApplyResources(this.chkSDKFunAll, "chkSDKFunAll");
		this.chkSDKFunAll.Name = "chkSDKFunAll";
		this.chkSDKFunAll.UseVisualStyleBackColor = true;
		resources.ApplyResources(this.label10, "label10");
		this.label10.Name = "label10";
		resources.ApplyResources(this.btnExample, "btnExample");
		this.btnExample.Name = "btnExample";
		this.btnExample.UseVisualStyleBackColor = true;
		this.btnExample.Click += new System.EventHandler(btnExample_Click);
		this.cboExample.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cboExample.FormattingEnabled = true;
		resources.ApplyResources(this.cboExample, "cboExample");
		this.cboExample.Name = "cboExample";
		resources.ApplyResources(this.label1, "label1");
		this.label1.Name = "label1";
		resources.ApplyResources(this.btnPrint1, "btnPrint1");
		this.btnPrint1.Name = "btnPrint1";
		this.btnPrint1.UseVisualStyleBackColor = true;
		this.btnPrint1.Click += new System.EventHandler(btnPrint1_Click);
		this.cboSDKFunction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cboSDKFunction.FormattingEnabled = true;
		resources.ApplyResources(this.cboSDKFunction, "cboSDKFunction");
		this.cboSDKFunction.Name = "cboSDKFunction";
		resources.ApplyResources(this.label6, "label6");
		this.label6.Name = "label6";
		resources.ApplyResources(this.tb_ProductMessage, "tb_ProductMessage");
		this.tb_ProductMessage.Name = "tb_ProductMessage";
		this.tb_ProductMessage.ReadOnly = true;
		resources.ApplyResources(this.bt_PrintBMP, "bt_PrintBMP");
		this.bt_PrintBMP.Name = "bt_PrintBMP";
		this.bt_PrintBMP.UseVisualStyleBackColor = true;
		this.bt_PrintBMP.Click += new System.EventHandler(bt_PrintBMP_Click);
		resources.ApplyResources(this.label45, "label45");
		this.label45.Name = "label45";
		resources.ApplyResources(this.bt_GetProductMessage, "bt_GetProductMessage");
		this.bt_GetProductMessage.Name = "bt_GetProductMessage";
		this.bt_GetProductMessage.UseVisualStyleBackColor = true;
		this.bt_GetProductMessage.Click += new System.EventHandler(bt_GetProductMessage_Click);
		resources.ApplyResources(this.bt_GetStatus, "bt_GetStatus");
		this.bt_GetStatus.Name = "bt_GetStatus";
		this.bt_GetStatus.UseVisualStyleBackColor = true;
		this.bt_GetStatus.Click += new System.EventHandler(bt_GetStatus_Click);
		resources.ApplyResources(this.tb_Status, "tb_Status");
		this.tb_Status.Name = "tb_Status";
		this.tb_Status.ReadOnly = true;
		this.tb_bmpFilePath.BackColor = System.Drawing.SystemColors.Window;
		resources.ApplyResources(this.tb_bmpFilePath, "tb_bmpFilePath");
		this.tb_bmpFilePath.Name = "tb_bmpFilePath";
		this.tb_bmpFilePath.DoubleClick += new System.EventHandler(tb_bmpFilePath_DoubleClick);
		resources.ApplyResources(this.label5, "label5");
		this.label5.Name = "label5";
		this.tabPage2.BackColor = System.Drawing.SystemColors.ButtonFace;
		this.tabPage2.Controls.Add(this.chkPrintIndex);
		this.tabPage2.Controls.Add(this.label4);
		this.tabPage2.Controls.Add(this.cboMulti2);
		this.tabPage2.Controls.Add(this.label27);
		this.tabPage2.Controls.Add(this.cboMulti1);
		this.tabPage2.Controls.Add(this.btnMulti1);
		this.tabPage2.Controls.Add(this.groupBox7);
		resources.ApplyResources(this.tabPage2, "tabPage2");
		this.tabPage2.Name = "tabPage2";
		resources.ApplyResources(this.chkPrintIndex, "chkPrintIndex");
		this.chkPrintIndex.Name = "chkPrintIndex";
		this.chkPrintIndex.UseVisualStyleBackColor = true;
		resources.ApplyResources(this.label4, "label4");
		this.label4.Name = "label4";
		this.cboMulti2.FormattingEnabled = true;
		resources.ApplyResources(this.cboMulti2, "cboMulti2");
		this.cboMulti2.Name = "cboMulti2";
		this.cboMulti2.SelectedIndexChanged += new System.EventHandler(cboMulti2_SelectedIndexChanged);
		resources.ApplyResources(this.label27, "label27");
		this.label27.Name = "label27";
		this.cboMulti1.FormattingEnabled = true;
		resources.ApplyResources(this.cboMulti1, "cboMulti1");
		this.cboMulti1.Name = "cboMulti1";
		resources.ApplyResources(this.btnMulti1, "btnMulti1");
		this.btnMulti1.Name = "btnMulti1";
		this.btnMulti1.UseVisualStyleBackColor = true;
		this.btnMulti1.Click += new System.EventHandler(btnMulti1_Click);
		this.groupBox7.Controls.Add(this.dgvRS);
		resources.ApplyResources(this.groupBox7, "groupBox7");
		this.groupBox7.Name = "groupBox7";
		this.groupBox7.TabStop = false;
		this.dgvRS.AllowUserToAddRows = false;
		this.dgvRS.AllowUserToDeleteRows = false;
		dataGridViewCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
		dataGridViewCellStyle.BackColor = System.Drawing.Color.SeaShell;
		this.dgvRS.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle;
		this.dgvRS.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
		this.dgvRS.BackgroundColor = System.Drawing.SystemColors.Control;
		this.dgvRS.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
		dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
		dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
		dataGridViewCellStyle2.Font = new System.Drawing.Font("宋体", 10.5f);
		dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
		dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
		dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
		dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
		this.dgvRS.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
		this.dgvRS.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dgvRS.Columns.AddRange(this.F_Cmd_ID, this.F_Select, this.F_Hex, this.F_Content, this.F_Comment, this.F_Seq, this.F_Sleep);
		resources.ApplyResources(this.dgvRS, "dgvRS");
		this.dgvRS.Name = "dgvRS";
		dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
		dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
		dataGridViewCellStyle3.Font = new System.Drawing.Font("宋体", 10.5f);
		dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
		dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
		dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
		dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
		this.dgvRS.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
		dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
		this.dgvRS.RowsDefaultCellStyle = dataGridViewCellStyle4;
		this.dgvRS.RowTemplate.Height = 28;
		this.dgvRS.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(dgvRS_DataError);
		this.F_Cmd_ID.DataPropertyName = "F_Cmd_ID";
		resources.ApplyResources(this.F_Cmd_ID, "F_Cmd_ID");
		this.F_Cmd_ID.Name = "F_Cmd_ID";
		this.F_Cmd_ID.ReadOnly = true;
		this.F_Cmd_ID.Resizable = System.Windows.Forms.DataGridViewTriState.False;
		this.F_Cmd_ID.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
		this.F_Select.DataPropertyName = "F_Select";
		dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
		dataGridViewCellStyle5.NullValue = false;
		dataGridViewCellStyle5.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
		this.F_Select.DefaultCellStyle = dataGridViewCellStyle5;
		resources.ApplyResources(this.F_Select, "F_Select");
		this.F_Select.Name = "F_Select";
		this.F_Select.Resizable = System.Windows.Forms.DataGridViewTriState.False;
		this.F_Hex.DataPropertyName = "F_Hex";
		dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
		dataGridViewCellStyle6.NullValue = false;
		dataGridViewCellStyle6.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
		this.F_Hex.DefaultCellStyle = dataGridViewCellStyle6;
		resources.ApplyResources(this.F_Hex, "F_Hex");
		this.F_Hex.Name = "F_Hex";
		this.F_Hex.Resizable = System.Windows.Forms.DataGridViewTriState.False;
		this.F_Content.DataPropertyName = "F_Content";
		resources.ApplyResources(this.F_Content, "F_Content");
		this.F_Content.Name = "F_Content";
		this.F_Content.Resizable = System.Windows.Forms.DataGridViewTriState.True;
		this.F_Content.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
		this.F_Comment.DataPropertyName = "F_Comment";
		resources.ApplyResources(this.F_Comment, "F_Comment");
		this.F_Comment.Name = "F_Comment";
		this.F_Comment.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
		this.F_Seq.DataPropertyName = "F_Seq";
		dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
		this.F_Seq.DefaultCellStyle = dataGridViewCellStyle7;
		resources.ApplyResources(this.F_Seq, "F_Seq");
		this.F_Seq.Name = "F_Seq";
		this.F_Seq.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
		this.F_Sleep.DataPropertyName = "F_Sleep";
		dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
		this.F_Sleep.DefaultCellStyle = dataGridViewCellStyle8;
		resources.ApplyResources(this.F_Sleep, "F_Sleep");
		this.F_Sleep.Name = "F_Sleep";
		this.F_Sleep.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
		this.tabPage3.BackColor = System.Drawing.Color.Transparent;
		this.tabPage3.Controls.Add(this.groupBox5);
		resources.ApplyResources(this.tabPage3, "tabPage3");
		this.tabPage3.Name = "tabPage3";
		this.groupBox5.BackColor = System.Drawing.SystemColors.ButtonFace;
		this.groupBox5.Controls.Add(this.groupBox12);
		this.groupBox5.Controls.Add(this.groupBox6);
		this.groupBox5.Controls.Add(this.groupBox4);
		this.groupBox5.Controls.Add(this.groupBox3);
		this.groupBox5.Controls.Add(this.groupBox2);
		resources.ApplyResources(this.groupBox5, "groupBox5");
		this.groupBox5.Name = "groupBox5";
		this.groupBox5.TabStop = false;
		this.groupBox12.Controls.Add(this.txtFontPath);
		this.groupBox12.Controls.Add(this.btnFontDownload);
		this.groupBox12.Controls.Add(this.label43);
		resources.ApplyResources(this.groupBox12, "groupBox12");
		this.groupBox12.Name = "groupBox12";
		this.groupBox12.TabStop = false;
		resources.ApplyResources(this.txtFontPath, "txtFontPath");
		this.txtFontPath.Name = "txtFontPath";
		this.txtFontPath.ReadOnly = true;
		resources.ApplyResources(this.btnFontDownload, "btnFontDownload");
		this.btnFontDownload.Name = "btnFontDownload";
		this.btnFontDownload.UseVisualStyleBackColor = true;
		this.btnFontDownload.Click += new System.EventHandler(btnFontDownload_Click);
		resources.ApplyResources(this.label43, "label43");
		this.label43.Name = "label43";
		this.groupBox6.Controls.Add(this.chkFontEpson);
		this.groupBox6.Controls.Add(this.label25);
		this.groupBox6.Controls.Add(this.comboBox10);
		this.groupBox6.Controls.Add(this.button1);
		resources.ApplyResources(this.groupBox6, "groupBox6");
		this.groupBox6.Name = "groupBox6";
		this.groupBox6.TabStop = false;
		resources.ApplyResources(this.chkFontEpson, "chkFontEpson");
		this.chkFontEpson.Name = "chkFontEpson";
		this.chkFontEpson.UseVisualStyleBackColor = true;
		resources.ApplyResources(this.label25, "label25");
		this.label25.Name = "label25";
		this.comboBox10.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.comboBox10.FormattingEnabled = true;
		resources.ApplyResources(this.comboBox10, "comboBox10");
		this.comboBox10.Name = "comboBox10";
		resources.ApplyResources(this.button1, "button1");
		this.button1.Name = "button1";
		this.button1.UseVisualStyleBackColor = true;
		this.button1.Click += new System.EventHandler(button1_Click);
		this.groupBox4.Controls.Add(this.chkFont2017All);
		this.groupBox4.Controls.Add(this.btnFont2017All);
		this.groupBox4.Controls.Add(this.label14);
		this.groupBox4.Controls.Add(this.comboBox8);
		this.groupBox4.Controls.Add(this.button2);
		this.groupBox4.Controls.Add(this.button3);
		resources.ApplyResources(this.groupBox4, "groupBox4");
		this.groupBox4.Name = "groupBox4";
		this.groupBox4.TabStop = false;
		resources.ApplyResources(this.chkFont2017All, "chkFont2017All");
		this.chkFont2017All.Name = "chkFont2017All";
		this.chkFont2017All.UseVisualStyleBackColor = true;
		resources.ApplyResources(this.btnFont2017All, "btnFont2017All");
		this.btnFont2017All.Name = "btnFont2017All";
		this.btnFont2017All.UseVisualStyleBackColor = true;
		this.btnFont2017All.Click += new System.EventHandler(btnFont2017All_Click);
		resources.ApplyResources(this.label14, "label14");
		this.label14.Name = "label14";
		this.comboBox8.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.comboBox8.FormattingEnabled = true;
		resources.ApplyResources(this.comboBox8, "comboBox8");
		this.comboBox8.Name = "comboBox8";
		resources.ApplyResources(this.button2, "button2");
		this.button2.Name = "button2";
		this.button2.UseVisualStyleBackColor = true;
		this.button2.Click += new System.EventHandler(button2_Click);
		resources.ApplyResources(this.button3, "button3");
		this.button3.Name = "button3";
		this.button3.UseVisualStyleBackColor = true;
		this.button3.Click += new System.EventHandler(button3_Click);
		this.groupBox3.Controls.Add(this.btnBlackTest);
		this.groupBox3.Controls.Add(this.label47);
		this.groupBox3.Controls.Add(this.tb_BlackAD);
		this.groupBox3.Controls.Add(this.label48);
		this.groupBox3.Controls.Add(this.btnSetBlackAD);
		this.groupBox3.Controls.Add(this.textBox1);
		this.groupBox3.Controls.Add(this.label42);
		this.groupBox3.Controls.Add(this.button7);
		this.groupBox3.Controls.Add(this.button6);
		this.groupBox3.Controls.Add(this.comboBox2);
		this.groupBox3.Controls.Add(this.label23);
		this.groupBox3.Controls.Add(this.label28);
		this.groupBox3.Controls.Add(this.textBox6);
		this.groupBox3.Controls.Add(this.label12);
		this.groupBox3.Controls.Add(this.label24);
		this.groupBox3.Controls.Add(this.label13);
		this.groupBox3.Controls.Add(this.button22);
		this.groupBox3.Controls.Add(this.comboBox5);
		this.groupBox3.Controls.Add(this.button21);
		this.groupBox3.Controls.Add(this.comboBox3);
		this.groupBox3.Controls.Add(this.comboBox18);
		this.groupBox3.Controls.Add(this.comboBox4);
		this.groupBox3.Controls.Add(this.label21);
		this.groupBox3.Controls.Add(this.button37);
		this.groupBox3.Controls.Add(this.label20);
		this.groupBox3.Controls.Add(this.button30);
		this.groupBox3.Controls.Add(this.label19);
		this.groupBox3.Controls.Add(this.button31);
		this.groupBox3.Controls.Add(this.button33);
		this.groupBox3.Controls.Add(this.textBox13);
		this.groupBox3.Controls.Add(this.label18);
		this.groupBox3.Controls.Add(this.button20);
		this.groupBox3.Controls.Add(this.label15);
		this.groupBox3.Controls.Add(this.comboBox7);
		this.groupBox3.Controls.Add(this.button18);
		this.groupBox3.Controls.Add(this.label17);
		this.groupBox3.Controls.Add(this.label16);
		this.groupBox3.Controls.Add(this.btnSetting6);
		this.groupBox3.Controls.Add(this.cboSetting6);
		resources.ApplyResources(this.groupBox3, "groupBox3");
		this.groupBox3.Name = "groupBox3";
		this.groupBox3.TabStop = false;
		resources.ApplyResources(this.btnBlackTest, "btnBlackTest");
		this.btnBlackTest.Name = "btnBlackTest";
		this.btnBlackTest.UseVisualStyleBackColor = true;
		this.btnBlackTest.Click += new System.EventHandler(btnBlackTest_Click);
		resources.ApplyResources(this.label47, "label47");
		this.label47.Name = "label47";
		resources.ApplyResources(this.tb_BlackAD, "tb_BlackAD");
		this.tb_BlackAD.Name = "tb_BlackAD";
		resources.ApplyResources(this.label48, "label48");
		this.label48.Name = "label48";
		resources.ApplyResources(this.btnSetBlackAD, "btnSetBlackAD");
		this.btnSetBlackAD.Name = "btnSetBlackAD";
		this.btnSetBlackAD.UseVisualStyleBackColor = true;
		this.btnSetBlackAD.Click += new System.EventHandler(btnSetBlackAD_Click);
		resources.ApplyResources(this.textBox1, "textBox1");
		this.textBox1.Name = "textBox1";
		resources.ApplyResources(this.label42, "label42");
		this.label42.Name = "label42";
		resources.ApplyResources(this.button7, "button7");
		this.button7.Name = "button7";
		this.button7.UseVisualStyleBackColor = true;
		this.button7.Click += new System.EventHandler(button7_Click);
		resources.ApplyResources(this.button6, "button6");
		this.button6.Name = "button6";
		this.button6.UseVisualStyleBackColor = true;
		this.button6.Click += new System.EventHandler(button6_Click);
		this.comboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.comboBox2.FormattingEnabled = true;
		this.comboBox2.Items.AddRange(new object[3]
		{
			resources.GetString("comboBox2.Items"),
			resources.GetString("comboBox2.Items1"),
			resources.GetString("comboBox2.Items2")
		});
		resources.ApplyResources(this.comboBox2, "comboBox2");
		this.comboBox2.Name = "comboBox2";
		resources.ApplyResources(this.label23, "label23");
		this.label23.Name = "label23";
		resources.ApplyResources(this.label28, "label28");
		this.label28.Name = "label28";
		resources.ApplyResources(this.textBox6, "textBox6");
		this.textBox6.Name = "textBox6";
		resources.ApplyResources(this.label12, "label12");
		this.label12.Name = "label12";
		resources.ApplyResources(this.label24, "label24");
		this.label24.Name = "label24";
		resources.ApplyResources(this.label13, "label13");
		this.label13.Name = "label13";
		resources.ApplyResources(this.button22, "button22");
		this.button22.Name = "button22";
		this.button22.UseVisualStyleBackColor = true;
		this.button22.Click += new System.EventHandler(button22_Click);
		this.comboBox5.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.comboBox5.FormattingEnabled = true;
		this.comboBox5.Items.AddRange(new object[2]
		{
			resources.GetString("comboBox5.Items"),
			resources.GetString("comboBox5.Items1")
		});
		resources.ApplyResources(this.comboBox5, "comboBox5");
		this.comboBox5.Name = "comboBox5";
		resources.ApplyResources(this.button21, "button21");
		this.button21.Name = "button21";
		this.button21.UseVisualStyleBackColor = true;
		this.button21.Click += new System.EventHandler(button21_Click);
		this.comboBox3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.comboBox3.FormattingEnabled = true;
		this.comboBox3.Items.AddRange(new object[2]
		{
			resources.GetString("comboBox3.Items"),
			resources.GetString("comboBox3.Items1")
		});
		resources.ApplyResources(this.comboBox3, "comboBox3");
		this.comboBox3.Name = "comboBox3";
		this.comboBox18.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.comboBox18.FormattingEnabled = true;
		this.comboBox18.Items.AddRange(new object[5]
		{
			resources.GetString("comboBox18.Items"),
			resources.GetString("comboBox18.Items1"),
			resources.GetString("comboBox18.Items2"),
			resources.GetString("comboBox18.Items3"),
			resources.GetString("comboBox18.Items4")
		});
		resources.ApplyResources(this.comboBox18, "comboBox18");
		this.comboBox18.Name = "comboBox18";
		this.comboBox4.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.comboBox4.FormattingEnabled = true;
		this.comboBox4.Items.AddRange(new object[2]
		{
			resources.GetString("comboBox4.Items"),
			resources.GetString("comboBox4.Items1")
		});
		resources.ApplyResources(this.comboBox4, "comboBox4");
		this.comboBox4.Name = "comboBox4";
		resources.ApplyResources(this.label21, "label21");
		this.label21.Name = "label21";
		resources.ApplyResources(this.button37, "button37");
		this.button37.Name = "button37";
		this.button37.UseVisualStyleBackColor = true;
		this.button37.Click += new System.EventHandler(button37_Click);
		resources.ApplyResources(this.label20, "label20");
		this.label20.Name = "label20";
		resources.ApplyResources(this.button30, "button30");
		this.button30.Name = "button30";
		this.button30.UseVisualStyleBackColor = true;
		this.button30.Click += new System.EventHandler(button30_Click);
		resources.ApplyResources(this.label19, "label19");
		this.label19.Name = "label19";
		resources.ApplyResources(this.button31, "button31");
		this.button31.Name = "button31";
		this.button31.UseVisualStyleBackColor = true;
		this.button31.Click += new System.EventHandler(button31_Click);
		resources.ApplyResources(this.button33, "button33");
		this.button33.Name = "button33";
		this.button33.UseVisualStyleBackColor = true;
		this.button33.Click += new System.EventHandler(button33_Click);
		resources.ApplyResources(this.textBox13, "textBox13");
		this.textBox13.Name = "textBox13";
		resources.ApplyResources(this.label18, "label18");
		this.label18.Name = "label18";
		resources.ApplyResources(this.button20, "button20");
		this.button20.Name = "button20";
		this.button20.UseVisualStyleBackColor = true;
		this.button20.Click += new System.EventHandler(button20_Click);
		resources.ApplyResources(this.label15, "label15");
		this.label15.Name = "label15";
		this.comboBox7.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.comboBox7.FormattingEnabled = true;
		this.comboBox7.Items.AddRange(new object[2]
		{
			resources.GetString("comboBox7.Items"),
			resources.GetString("comboBox7.Items1")
		});
		resources.ApplyResources(this.comboBox7, "comboBox7");
		this.comboBox7.Name = "comboBox7";
		resources.ApplyResources(this.button18, "button18");
		this.button18.Name = "button18";
		this.button18.UseVisualStyleBackColor = true;
		this.button18.Click += new System.EventHandler(button18_Click);
		resources.ApplyResources(this.label17, "label17");
		this.label17.Name = "label17";
		resources.ApplyResources(this.label16, "label16");
		this.label16.Name = "label16";
		resources.ApplyResources(this.btnSetting6, "btnSetting6");
		this.btnSetting6.Name = "btnSetting6";
		this.btnSetting6.UseVisualStyleBackColor = true;
		this.btnSetting6.Click += new System.EventHandler(btnSetting6_Click);
		this.cboSetting6.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cboSetting6.FormattingEnabled = true;
		resources.ApplyResources(this.cboSetting6, "cboSetting6");
		this.cboSetting6.Name = "cboSetting6";
		this.groupBox2.Controls.Add(this.cboFontLib);
		this.groupBox2.Controls.Add(this.cb_Character);
		this.groupBox2.Controls.Add(this.chkFontB);
		this.groupBox2.Controls.Add(this.button5);
		this.groupBox2.Controls.Add(this.label22);
		this.groupBox2.Controls.Add(this.comboBox9);
		this.groupBox2.Controls.Add(this.label9);
		resources.ApplyResources(this.groupBox2, "groupBox2");
		this.groupBox2.Name = "groupBox2";
		this.groupBox2.TabStop = false;
		this.cboFontLib.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cboFontLib.FormattingEnabled = true;
		resources.ApplyResources(this.cboFontLib, "cboFontLib");
		this.cboFontLib.Name = "cboFontLib";
		this.cb_Character.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cb_Character.FormattingEnabled = true;
		resources.ApplyResources(this.cb_Character, "cb_Character");
		this.cb_Character.Name = "cb_Character";
		resources.ApplyResources(this.chkFontB, "chkFontB");
		this.chkFontB.Name = "chkFontB";
		this.chkFontB.UseVisualStyleBackColor = true;
		resources.ApplyResources(this.button5, "button5");
		this.button5.Name = "button5";
		this.button5.UseVisualStyleBackColor = true;
		this.button5.Click += new System.EventHandler(button5_Click);
		resources.ApplyResources(this.label22, "label22");
		this.label22.Name = "label22";
		this.comboBox9.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.comboBox9.FormattingEnabled = true;
		resources.ApplyResources(this.comboBox9, "comboBox9");
		this.comboBox9.Name = "comboBox9";
		resources.ApplyResources(this.label9, "label9");
		this.label9.Name = "label9";
		this.tabPage4.BackColor = System.Drawing.SystemColors.ButtonFace;
		this.tabPage4.Controls.Add(this.btnSetNV);
		this.tabPage4.Controls.Add(this.label26);
		this.tabPage4.Controls.Add(this.cboNVIndex);
		this.tabPage4.Controls.Add(this.btnNVPrint);
		this.tabPage4.Controls.Add(this.groupBox8);
		resources.ApplyResources(this.tabPage4, "tabPage4");
		this.tabPage4.Name = "tabPage4";
		resources.ApplyResources(this.btnSetNV, "btnSetNV");
		this.btnSetNV.Name = "btnSetNV";
		this.btnSetNV.UseVisualStyleBackColor = true;
		this.btnSetNV.Click += new System.EventHandler(btnSetNV_Click);
		resources.ApplyResources(this.label26, "label26");
		this.label26.Name = "label26";
		this.cboNVIndex.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this.cboNVIndex.FormattingEnabled = true;
		resources.ApplyResources(this.cboNVIndex, "cboNVIndex");
		this.cboNVIndex.Name = "cboNVIndex";
		resources.ApplyResources(this.btnNVPrint, "btnNVPrint");
		this.btnNVPrint.Name = "btnNVPrint";
		this.btnNVPrint.UseVisualStyleBackColor = true;
		this.btnNVPrint.Click += new System.EventHandler(btnNVPrint_Click);
		this.groupBox8.BackColor = System.Drawing.SystemColors.ButtonFace;
		this.groupBox8.Controls.Add(this.dgvNV);
		resources.ApplyResources(this.groupBox8, "groupBox8");
		this.groupBox8.Name = "groupBox8";
		this.groupBox8.TabStop = false;
		this.dgvNV.AllowUserToAddRows = false;
		this.dgvNV.AllowUserToDeleteRows = false;
		dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
		dataGridViewCellStyle9.BackColor = System.Drawing.Color.SeaShell;
		this.dgvNV.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle9;
		this.dgvNV.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
		this.dgvNV.BackgroundColor = System.Drawing.SystemColors.Control;
		this.dgvNV.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
		dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
		dataGridViewCellStyle10.BackColor = System.Drawing.SystemColors.Control;
		dataGridViewCellStyle10.Font = new System.Drawing.Font("宋体", 10.5f);
		dataGridViewCellStyle10.ForeColor = System.Drawing.SystemColors.WindowText;
		dataGridViewCellStyle10.SelectionBackColor = System.Drawing.SystemColors.Highlight;
		dataGridViewCellStyle10.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
		dataGridViewCellStyle10.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
		this.dgvNV.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle10;
		this.dgvNV.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dgvNV.Columns.AddRange(this.dataGridViewTextBoxColumn1, this.dataGridViewTextBoxColumn2, this.F_FilePath, this.dataGridViewTextBoxColumn4);
		resources.ApplyResources(this.dgvNV, "dgvNV");
		this.dgvNV.Name = "dgvNV";
		dataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
		dataGridViewCellStyle11.BackColor = System.Drawing.SystemColors.Control;
		dataGridViewCellStyle11.Font = new System.Drawing.Font("宋体", 10.5f);
		dataGridViewCellStyle11.ForeColor = System.Drawing.SystemColors.WindowText;
		dataGridViewCellStyle11.SelectionBackColor = System.Drawing.SystemColors.Highlight;
		dataGridViewCellStyle11.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
		dataGridViewCellStyle11.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
		this.dgvNV.RowHeadersDefaultCellStyle = dataGridViewCellStyle11;
		dataGridViewCellStyle12.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
		this.dgvNV.RowsDefaultCellStyle = dataGridViewCellStyle12;
		this.dgvNV.RowTemplate.Height = 28;
		this.dgvNV.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(dgvNV_CellDoubleClick);
		this.dataGridViewTextBoxColumn1.DataPropertyName = "F_NV_ID";
		resources.ApplyResources(this.dataGridViewTextBoxColumn1, "dataGridViewTextBoxColumn1");
		this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
		this.dataGridViewTextBoxColumn1.ReadOnly = true;
		this.dataGridViewTextBoxColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.False;
		this.dataGridViewTextBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
		this.dataGridViewTextBoxColumn2.DataPropertyName = "F_NVIndex";
		resources.ApplyResources(this.dataGridViewTextBoxColumn2, "dataGridViewTextBoxColumn2");
		this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
		this.dataGridViewTextBoxColumn2.ReadOnly = true;
		this.dataGridViewTextBoxColumn2.Resizable = System.Windows.Forms.DataGridViewTriState.True;
		this.dataGridViewTextBoxColumn2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
		this.F_FilePath.DataPropertyName = "F_FilePath";
		resources.ApplyResources(this.F_FilePath, "F_FilePath");
		this.F_FilePath.Name = "F_FilePath";
		this.F_FilePath.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
		this.dataGridViewTextBoxColumn4.DataPropertyName = "F_Comment";
		dataGridViewCellStyle13.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
		this.dataGridViewTextBoxColumn4.DefaultCellStyle = dataGridViewCellStyle13;
		resources.ApplyResources(this.dataGridViewTextBoxColumn4, "dataGridViewTextBoxColumn4");
		this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
		this.dataGridViewTextBoxColumn4.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
		this.tabPage5.BackColor = System.Drawing.SystemColors.ButtonFace;
		this.tabPage5.Controls.Add(this.groupBox11);
		this.tabPage5.Controls.Add(this.groupBox10);
		this.tabPage5.Controls.Add(this.groupBox9);
		resources.ApplyResources(this.tabPage5, "tabPage5");
		this.tabPage5.Name = "tabPage5";
		this.groupBox11.Controls.Add(this.btnWifiRead);
		this.groupBox11.Controls.Add(this.btnWifiDNS);
		this.groupBox11.Controls.Add(this.btnWifiGateway);
		this.groupBox11.Controls.Add(this.btnWifiMask);
		this.groupBox11.Controls.Add(this.btnWifiIPAddr);
		this.groupBox11.Controls.Add(this.txtWifiDNS);
		this.groupBox11.Controls.Add(this.txtWifiGateway);
		this.groupBox11.Controls.Add(this.txtWifiMask);
		this.groupBox11.Controls.Add(this.txtWifiIPAddr);
		this.groupBox11.Controls.Add(this.label41);
		this.groupBox11.Controls.Add(this.label40);
		this.groupBox11.Controls.Add(this.label39);
		this.groupBox11.Controls.Add(this.label38);
		resources.ApplyResources(this.groupBox11, "groupBox11");
		this.groupBox11.Name = "groupBox11";
		this.groupBox11.TabStop = false;
		resources.ApplyResources(this.btnWifiRead, "btnWifiRead");
		this.btnWifiRead.Name = "btnWifiRead";
		this.btnWifiRead.UseVisualStyleBackColor = true;
		this.btnWifiRead.Click += new System.EventHandler(btnWifiRead_Click);
		resources.ApplyResources(this.btnWifiDNS, "btnWifiDNS");
		this.btnWifiDNS.Name = "btnWifiDNS";
		this.btnWifiDNS.UseVisualStyleBackColor = true;
		this.btnWifiDNS.Click += new System.EventHandler(btnWifiDNS_Click);
		resources.ApplyResources(this.btnWifiGateway, "btnWifiGateway");
		this.btnWifiGateway.Name = "btnWifiGateway";
		this.btnWifiGateway.UseVisualStyleBackColor = true;
		this.btnWifiGateway.Click += new System.EventHandler(btnWifiGateway_Click);
		resources.ApplyResources(this.btnWifiMask, "btnWifiMask");
		this.btnWifiMask.Name = "btnWifiMask";
		this.btnWifiMask.UseVisualStyleBackColor = true;
		this.btnWifiMask.Click += new System.EventHandler(btnWifiMask_Click);
		resources.ApplyResources(this.btnWifiIPAddr, "btnWifiIPAddr");
		this.btnWifiIPAddr.Name = "btnWifiIPAddr";
		this.btnWifiIPAddr.UseVisualStyleBackColor = true;
		this.btnWifiIPAddr.Click += new System.EventHandler(btnWifiIPAddr_Click);
		resources.ApplyResources(this.txtWifiDNS, "txtWifiDNS");
		this.txtWifiDNS.Name = "txtWifiDNS";
		resources.ApplyResources(this.txtWifiGateway, "txtWifiGateway");
		this.txtWifiGateway.Name = "txtWifiGateway";
		resources.ApplyResources(this.txtWifiMask, "txtWifiMask");
		this.txtWifiMask.Name = "txtWifiMask";
		resources.ApplyResources(this.txtWifiIPAddr, "txtWifiIPAddr");
		this.txtWifiIPAddr.Name = "txtWifiIPAddr";
		resources.ApplyResources(this.label41, "label41");
		this.label41.Name = "label41";
		resources.ApplyResources(this.label40, "label40");
		this.label40.Name = "label40";
		resources.ApplyResources(this.label39, "label39");
		this.label39.Name = "label39";
		resources.ApplyResources(this.label38, "label38");
		this.label38.Name = "label38";
		this.groupBox10.Controls.Add(this.txtWifiSubscribe3);
		this.groupBox10.Controls.Add(this.label54);
		this.groupBox10.Controls.Add(this.txtWifiSubscribe2);
		this.groupBox10.Controls.Add(this.label53);
		this.groupBox10.Controls.Add(this.txtWifiPublish3);
		this.groupBox10.Controls.Add(this.label52);
		this.groupBox10.Controls.Add(this.txtWifiPublish2);
		this.groupBox10.Controls.Add(this.label49);
		this.groupBox10.Controls.Add(this.txtWifiMQTTPassword);
		this.groupBox10.Controls.Add(this.btnWifiSet1);
		this.groupBox10.Controls.Add(this.btnWifiSet6);
		this.groupBox10.Controls.Add(this.label7);
		this.groupBox10.Controls.Add(this.txtWifiSubscribe1);
		this.groupBox10.Controls.Add(this.txtWifiIP);
		this.groupBox10.Controls.Add(this.label36);
		this.groupBox10.Controls.Add(this.label8);
		this.groupBox10.Controls.Add(this.txtWifiPublish1);
		this.groupBox10.Controls.Add(this.txtWifiPort);
		this.groupBox10.Controls.Add(this.label37);
		this.groupBox10.Controls.Add(this.label30);
		this.groupBox10.Controls.Add(this.txtWifiMQTTClientName);
		this.groupBox10.Controls.Add(this.cboWIFIModel);
		this.groupBox10.Controls.Add(this.label35);
		this.groupBox10.Controls.Add(this.btnWifiSet3);
		this.groupBox10.Controls.Add(this.btnWifiSet5);
		this.groupBox10.Controls.Add(this.label32);
		this.groupBox10.Controls.Add(this.txtWifiMQTTPort);
		this.groupBox10.Controls.Add(this.txtWifiMQTTClientID);
		this.groupBox10.Controls.Add(this.label33);
		this.groupBox10.Controls.Add(this.label31);
		this.groupBox10.Controls.Add(this.txtWifiMQTTIP);
		this.groupBox10.Controls.Add(this.btnWifiSet4);
		this.groupBox10.Controls.Add(this.label34);
		resources.ApplyResources(this.groupBox10, "groupBox10");
		this.groupBox10.Name = "groupBox10";
		this.groupBox10.TabStop = false;
		resources.ApplyResources(this.txtWifiSubscribe3, "txtWifiSubscribe3");
		this.txtWifiSubscribe3.Name = "txtWifiSubscribe3";
		resources.ApplyResources(this.label54, "label54");
		this.label54.Name = "label54";
		resources.ApplyResources(this.txtWifiSubscribe2, "txtWifiSubscribe2");
		this.txtWifiSubscribe2.Name = "txtWifiSubscribe2";
		resources.ApplyResources(this.label53, "label53");
		this.label53.Name = "label53";
		resources.ApplyResources(this.txtWifiPublish3, "txtWifiPublish3");
		this.txtWifiPublish3.Name = "txtWifiPublish3";
		resources.ApplyResources(this.label52, "label52");
		this.label52.Name = "label52";
		resources.ApplyResources(this.txtWifiPublish2, "txtWifiPublish2");
		this.txtWifiPublish2.Name = "txtWifiPublish2";
		resources.ApplyResources(this.label49, "label49");
		this.label49.Name = "label49";
		resources.ApplyResources(this.txtWifiMQTTPassword, "txtWifiMQTTPassword");
		this.txtWifiMQTTPassword.Name = "txtWifiMQTTPassword";
		resources.ApplyResources(this.btnWifiSet1, "btnWifiSet1");
		this.btnWifiSet1.Name = "btnWifiSet1";
		this.btnWifiSet1.UseVisualStyleBackColor = true;
		this.btnWifiSet1.Click += new System.EventHandler(btnWIFISet1_Click);
		resources.ApplyResources(this.btnWifiSet6, "btnWifiSet6");
		this.btnWifiSet6.Name = "btnWifiSet6";
		this.btnWifiSet6.UseVisualStyleBackColor = true;
		this.btnWifiSet6.Click += new System.EventHandler(btnWifiSet6_Click);
		resources.ApplyResources(this.label7, "label7");
		this.label7.Name = "label7";
		resources.ApplyResources(this.txtWifiSubscribe1, "txtWifiSubscribe1");
		this.txtWifiSubscribe1.Name = "txtWifiSubscribe1";
		resources.ApplyResources(this.txtWifiIP, "txtWifiIP");
		this.txtWifiIP.Name = "txtWifiIP";
		resources.ApplyResources(this.label36, "label36");
		this.label36.Name = "label36";
		resources.ApplyResources(this.label8, "label8");
		this.label8.Name = "label8";
		resources.ApplyResources(this.txtWifiPublish1, "txtWifiPublish1");
		this.txtWifiPublish1.Name = "txtWifiPublish1";
		resources.ApplyResources(this.txtWifiPort, "txtWifiPort");
		this.txtWifiPort.Name = "txtWifiPort";
		resources.ApplyResources(this.label37, "label37");
		this.label37.Name = "label37";
		resources.ApplyResources(this.label30, "label30");
		this.label30.Name = "label30";
		resources.ApplyResources(this.txtWifiMQTTClientName, "txtWifiMQTTClientName");
		this.txtWifiMQTTClientName.Name = "txtWifiMQTTClientName";
		this.cboWIFIModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		resources.ApplyResources(this.cboWIFIModel, "cboWIFIModel");
		this.cboWIFIModel.FormattingEnabled = true;
		this.cboWIFIModel.Name = "cboWIFIModel";
		resources.ApplyResources(this.label35, "label35");
		this.label35.Name = "label35";
		resources.ApplyResources(this.btnWifiSet3, "btnWifiSet3");
		this.btnWifiSet3.Name = "btnWifiSet3";
		this.btnWifiSet3.UseVisualStyleBackColor = true;
		this.btnWifiSet3.Click += new System.EventHandler(btnWifiSet3_Click);
		resources.ApplyResources(this.btnWifiSet5, "btnWifiSet5");
		this.btnWifiSet5.Name = "btnWifiSet5";
		this.btnWifiSet5.UseVisualStyleBackColor = true;
		this.btnWifiSet5.Click += new System.EventHandler(btnWifiSet5_Click);
		resources.ApplyResources(this.label32, "label32");
		this.label32.Name = "label32";
		resources.ApplyResources(this.txtWifiMQTTPort, "txtWifiMQTTPort");
		this.txtWifiMQTTPort.Name = "txtWifiMQTTPort";
		resources.ApplyResources(this.txtWifiMQTTClientID, "txtWifiMQTTClientID");
		this.txtWifiMQTTClientID.Name = "txtWifiMQTTClientID";
		resources.ApplyResources(this.label33, "label33");
		this.label33.Name = "label33";
		resources.ApplyResources(this.label31, "label31");
		this.label31.Name = "label31";
		resources.ApplyResources(this.txtWifiMQTTIP, "txtWifiMQTTIP");
		this.txtWifiMQTTIP.Name = "txtWifiMQTTIP";
		resources.ApplyResources(this.btnWifiSet4, "btnWifiSet4");
		this.btnWifiSet4.Name = "btnWifiSet4";
		this.btnWifiSet4.UseVisualStyleBackColor = true;
		this.btnWifiSet4.Click += new System.EventHandler(btnWifiSet4_Click);
		resources.ApplyResources(this.label34, "label34");
		this.label34.Name = "label34";
		this.groupBox9.Controls.Add(this.txtWifiWan);
		this.groupBox9.Controls.Add(this.label29);
		this.groupBox9.Controls.Add(this.btnWifiSet2);
		this.groupBox9.Controls.Add(this.label11);
		this.groupBox9.Controls.Add(this.txtWifiPassword);
		resources.ApplyResources(this.groupBox9, "groupBox9");
		this.groupBox9.Name = "groupBox9";
		this.groupBox9.TabStop = false;
		resources.ApplyResources(this.txtWifiWan, "txtWifiWan");
		this.txtWifiWan.Name = "txtWifiWan";
		resources.ApplyResources(this.label29, "label29");
		this.label29.Name = "label29";
		resources.ApplyResources(this.btnWifiSet2, "btnWifiSet2");
		this.btnWifiSet2.Name = "btnWifiSet2";
		this.btnWifiSet2.UseVisualStyleBackColor = true;
		this.btnWifiSet2.Click += new System.EventHandler(btnWifiSet2_Click);
		resources.ApplyResources(this.label11, "label11");
		this.label11.Name = "label11";
		resources.ApplyResources(this.txtWifiPassword, "txtWifiPassword");
		this.txtWifiPassword.Name = "txtWifiPassword";
		this.tabPage6.BackColor = System.Drawing.SystemColors.ButtonFace;
		this.tabPage6.Controls.Add(this.groupBox13);
		resources.ApplyResources(this.tabPage6, "tabPage6");
		this.tabPage6.Name = "tabPage6";
		this.groupBox13.Controls.Add(this.txtSpeedCount);
		this.groupBox13.Controls.Add(this.txtSpeedData);
		this.groupBox13.Controls.Add(this.label51);
		this.groupBox13.Controls.Add(this.label50);
		this.groupBox13.Controls.Add(this.btnSpeedStart);
		resources.ApplyResources(this.groupBox13, "groupBox13");
		this.groupBox13.Name = "groupBox13";
		this.groupBox13.TabStop = false;
		resources.ApplyResources(this.txtSpeedCount, "txtSpeedCount");
		this.txtSpeedCount.Name = "txtSpeedCount";
		resources.ApplyResources(this.txtSpeedData, "txtSpeedData");
		this.txtSpeedData.Name = "txtSpeedData";
		resources.ApplyResources(this.label51, "label51");
		this.label51.Name = "label51";
		resources.ApplyResources(this.label50, "label50");
		this.label50.Name = "label50";
		resources.ApplyResources(this.btnSpeedStart, "btnSpeedStart");
		this.btnSpeedStart.Name = "btnSpeedStart";
		this.btnSpeedStart.UseVisualStyleBackColor = true;
		this.btnSpeedStart.Click += new System.EventHandler(btnSpeedStart_Click);
		this.tabPage7.Controls.Add(this.cboDC01);
		this.tabPage7.Controls.Add(this.lblDC01);
		this.tabPage7.Controls.Add(this.btnDCPrint);
		this.tabPage7.Controls.Add(this.groupBox14);
		resources.ApplyResources(this.tabPage7, "tabPage7");
		this.tabPage7.Name = "tabPage7";
		this.cboDC01.FormattingEnabled = true;
		resources.ApplyResources(this.cboDC01, "cboDC01");
		this.cboDC01.Name = "cboDC01";
		this.cboDC01.SelectedIndexChanged += new System.EventHandler(cboDC01_SelectedIndexChanged);
		resources.ApplyResources(this.lblDC01, "lblDC01");
		this.lblDC01.Name = "lblDC01";
		resources.ApplyResources(this.btnDCPrint, "btnDCPrint");
		this.btnDCPrint.Name = "btnDCPrint";
		this.btnDCPrint.UseVisualStyleBackColor = true;
		this.btnDCPrint.Click += new System.EventHandler(btnDCPrint_Click);
		this.groupBox14.Controls.Add(this.dgvDC01);
		resources.ApplyResources(this.groupBox14, "groupBox14");
		this.groupBox14.Name = "groupBox14";
		this.groupBox14.TabStop = false;
		this.dgvDC01.AllowUserToAddRows = false;
		this.dgvDC01.AllowUserToDeleteRows = false;
		dataGridViewCellStyle14.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
		dataGridViewCellStyle14.BackColor = System.Drawing.Color.SeaShell;
		this.dgvDC01.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle14;
		this.dgvDC01.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
		this.dgvDC01.BackgroundColor = System.Drawing.SystemColors.Control;
		this.dgvDC01.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
		dataGridViewCellStyle15.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
		dataGridViewCellStyle15.BackColor = System.Drawing.SystemColors.Control;
		dataGridViewCellStyle15.Font = new System.Drawing.Font("宋体", 10.5f);
		dataGridViewCellStyle15.ForeColor = System.Drawing.SystemColors.WindowText;
		dataGridViewCellStyle15.SelectionBackColor = System.Drawing.SystemColors.Highlight;
		dataGridViewCellStyle15.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
		dataGridViewCellStyle15.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
		this.dgvDC01.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle15;
		this.dgvDC01.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.dgvDC01.Columns.AddRange(this.dataGridViewTextBoxColumn3, this.dataGridViewCheckBoxColumn1, this.F_DCHex, this.Color, this.F_DCContent, this.F_DCComment, this.dataGridViewTextBoxColumn7, this.dataGridViewTextBoxColumn8);
		resources.ApplyResources(this.dgvDC01, "dgvDC01");
		this.dgvDC01.Name = "dgvDC01";
		dataGridViewCellStyle16.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
		dataGridViewCellStyle16.BackColor = System.Drawing.SystemColors.Control;
		dataGridViewCellStyle16.Font = new System.Drawing.Font("宋体", 10.5f);
		dataGridViewCellStyle16.ForeColor = System.Drawing.SystemColors.WindowText;
		dataGridViewCellStyle16.SelectionBackColor = System.Drawing.SystemColors.Highlight;
		dataGridViewCellStyle16.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
		dataGridViewCellStyle16.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
		this.dgvDC01.RowHeadersDefaultCellStyle = dataGridViewCellStyle16;
		dataGridViewCellStyle17.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
		this.dgvDC01.RowsDefaultCellStyle = dataGridViewCellStyle17;
		this.dgvDC01.RowTemplate.Height = 28;
		this.dgvDC01.CellMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(dgvDC01_CellMouseDoubleClick);
		this.dataGridViewTextBoxColumn3.DataPropertyName = "F_Cmd_ID";
		resources.ApplyResources(this.dataGridViewTextBoxColumn3, "dataGridViewTextBoxColumn3");
		this.dataGridViewTextBoxColumn3.MaxInputLength = 262144;
		this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
		this.dataGridViewTextBoxColumn3.ReadOnly = true;
		this.dataGridViewTextBoxColumn3.Resizable = System.Windows.Forms.DataGridViewTriState.False;
		this.dataGridViewTextBoxColumn3.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
		this.dataGridViewCheckBoxColumn1.DataPropertyName = "F_Select";
		dataGridViewCellStyle18.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
		dataGridViewCellStyle18.NullValue = false;
		dataGridViewCellStyle18.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
		this.dataGridViewCheckBoxColumn1.DefaultCellStyle = dataGridViewCellStyle18;
		resources.ApplyResources(this.dataGridViewCheckBoxColumn1, "dataGridViewCheckBoxColumn1");
		this.dataGridViewCheckBoxColumn1.Name = "dataGridViewCheckBoxColumn1";
		this.dataGridViewCheckBoxColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.False;
		this.F_DCHex.DataPropertyName = "F_Hex";
		dataGridViewCellStyle19.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
		dataGridViewCellStyle19.NullValue = false;
		dataGridViewCellStyle19.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
		this.F_DCHex.DefaultCellStyle = dataGridViewCellStyle19;
		resources.ApplyResources(this.F_DCHex, "F_DCHex");
		this.F_DCHex.Name = "F_DCHex";
		this.F_DCHex.Resizable = System.Windows.Forms.DataGridViewTriState.False;
		this.Color.DataPropertyName = "F_Color";
		dataGridViewCellStyle20.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
		dataGridViewCellStyle20.NullValue = false;
		dataGridViewCellStyle20.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
		this.Color.DefaultCellStyle = dataGridViewCellStyle20;
		resources.ApplyResources(this.Color, "Color");
		this.Color.Name = "Color";
		this.F_DCContent.DataPropertyName = "F_Content";
		resources.ApplyResources(this.F_DCContent, "F_DCContent");
		this.F_DCContent.MaxInputLength = 262144;
		this.F_DCContent.Name = "F_DCContent";
		this.F_DCContent.Resizable = System.Windows.Forms.DataGridViewTriState.True;
		this.F_DCContent.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
		this.F_DCComment.DataPropertyName = "F_Comment";
		resources.ApplyResources(this.F_DCComment, "F_DCComment");
		this.F_DCComment.Name = "F_DCComment";
		this.F_DCComment.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
		this.dataGridViewTextBoxColumn7.DataPropertyName = "F_Seq";
		dataGridViewCellStyle21.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
		this.dataGridViewTextBoxColumn7.DefaultCellStyle = dataGridViewCellStyle21;
		resources.ApplyResources(this.dataGridViewTextBoxColumn7, "dataGridViewTextBoxColumn7");
		this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
		this.dataGridViewTextBoxColumn7.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
		this.dataGridViewTextBoxColumn8.DataPropertyName = "F_Sleep";
		dataGridViewCellStyle22.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
		this.dataGridViewTextBoxColumn8.DefaultCellStyle = dataGridViewCellStyle22;
		resources.ApplyResources(this.dataGridViewTextBoxColumn8, "dataGridViewTextBoxColumn8");
		this.dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
		this.dataGridViewTextBoxColumn8.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
		this.languaToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[2] { this.englishToolStripMenuItem, this.ToolStripMenuItem });
		this.languaToolStripMenuItem.Name = "languaToolStripMenuItem";
		resources.ApplyResources(this.languaToolStripMenuItem, "languaToolStripMenuItem");
		this.englishToolStripMenuItem.Name = "englishToolStripMenuItem";
		resources.ApplyResources(this.englishToolStripMenuItem, "englishToolStripMenuItem");
		this.englishToolStripMenuItem.Click += new System.EventHandler(englishToolStripMenuItem_Click);
		this.ToolStripMenuItem.Name = "ToolStripMenuItem";
		resources.ApplyResources(this.ToolStripMenuItem, "ToolStripMenuItem");
		this.ToolStripMenuItem.Click += new System.EventHandler(ToolStripMenuItem_Click);
		this.menuStrip1.BackColor = System.Drawing.SystemColors.ButtonFace;
		this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[1] { this.languaToolStripMenuItem });
		resources.ApplyResources(this.menuStrip1, "menuStrip1");
		this.menuStrip1.Name = "menuStrip1";
		resources.ApplyResources(this, "$this");
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.Controls.Add(this.tabControl1);
		base.Controls.Add(this.groupBox1);
		base.Controls.Add(this.menuStrip1);
		base.KeyPreview = true;
		base.MainMenuStrip = this.menuStrip1;
		base.MaximizeBox = false;
		base.Name = "FrmMain";
		base.Load += new System.EventHandler(FrmMain_Load);
		this.groupBox1.ResumeLayout(false);
		this.groupBox1.PerformLayout();
		this.tabControl1.ResumeLayout(false);
		this.tabPage1.ResumeLayout(false);
		this.gb_Receive.ResumeLayout(false);
		this.gb_Receive.PerformLayout();
		this.gb_Send.ResumeLayout(false);
		this.gb_Send.PerformLayout();
		this.gb_BasicTest.ResumeLayout(false);
		this.gb_BasicTest.PerformLayout();
		this.tabPage2.ResumeLayout(false);
		this.tabPage2.PerformLayout();
		this.groupBox7.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dgvRS).EndInit();
		this.tabPage3.ResumeLayout(false);
		this.groupBox5.ResumeLayout(false);
		this.groupBox12.ResumeLayout(false);
		this.groupBox12.PerformLayout();
		this.groupBox6.ResumeLayout(false);
		this.groupBox6.PerformLayout();
		this.groupBox4.ResumeLayout(false);
		this.groupBox4.PerformLayout();
		this.groupBox3.ResumeLayout(false);
		this.groupBox3.PerformLayout();
		this.groupBox2.ResumeLayout(false);
		this.groupBox2.PerformLayout();
		this.tabPage4.ResumeLayout(false);
		this.tabPage4.PerformLayout();
		this.groupBox8.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dgvNV).EndInit();
		this.tabPage5.ResumeLayout(false);
		this.groupBox11.ResumeLayout(false);
		this.groupBox11.PerformLayout();
		this.groupBox10.ResumeLayout(false);
		this.groupBox10.PerformLayout();
		this.groupBox9.ResumeLayout(false);
		this.groupBox9.PerformLayout();
		this.tabPage6.ResumeLayout(false);
		this.groupBox13.ResumeLayout(false);
		this.groupBox13.PerformLayout();
		this.tabPage7.ResumeLayout(false);
		this.tabPage7.PerformLayout();
		this.groupBox14.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)this.dgvDC01).EndInit();
		this.menuStrip1.ResumeLayout(false);
		this.menuStrip1.PerformLayout();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
