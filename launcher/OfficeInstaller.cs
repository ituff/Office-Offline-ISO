using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

public class OfficeInstaller : Form
{
    private CheckBox[] productChecks;
    private RadioButton[] langRadios;
    private Button btnInstall;
    private PictureBox picQR;
    private Label lblTitle, lblProduct, lblLang, lblQR, lblVersion;
    private ComboBox cmbLang;
    private string officeVersion = "";

    private readonly string[] productIds = {
        "ProPlus2024Volume",
        "VisioStd2024Volume",
        "VisioPro2024Volume",
        "ProjectStd2024Volume",
        "ProjectPro2024Volume"
    };

    private readonly string[][] productNames = {
        new[] { "Office LTSC 专业增强版 2024", "Office LTSC Professional Plus 2024", "Office LTSC プロフェッショナル プラス 2024" },
        new[] { "Visio LTSC 标准版 2024", "Visio LTSC Standard 2024", "Visio LTSC スタンダード 2024" },
        new[] { "Visio LTSC 专业版 2024", "Visio LTSC Professional 2024", "Visio LTSC プロフェッショナル 2024" },
        new[] { "Project LTSC 标准版 2024", "Project LTSC Standard 2024", "Project LTSC スタンダード 2024" },
        new[] { "Project LTSC 专业版 2024", "Project LTSC Professional 2024", "Project LTSC プロフェッショナル 2024" }
    };

    private readonly string[] langCodes = { "zh-cn", "en-us", "ja-jp" };
    private readonly string[][] langNames = {
        new[] { "中文", "English", "日本語" },
        new[] { "Chinese", "English", "Japanese" },
        new[] { "中国語", "英語", "日本語" }
    };

    private string[] strTitle, strProduct, strLang, strInstall, strQR;
    private string[] strSelectLang, strNoSelect, strNoProduct, strInstalling, strDone, strVersion;
    private int langIndex = 0;

    public OfficeInstaller()
    {
        DetectLanguage();
        DetectOfficeVersion();
        InitStrings();
        InitUI();
    }

    private void DetectLanguage()
    {
        string sysLang = CultureInfo.InstalledUICulture.Name.ToLower();
        if (sysLang.StartsWith("zh")) langIndex = 0;
        else if (sysLang.StartsWith("ja")) langIndex = 2;
        else langIndex = 1;
    }

    private void DetectOfficeVersion()
    {
        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string dataDir = Path.Combine(baseDir, "Office", "Data");
            if (Directory.Exists(dataDir))
            {
                foreach (string dir in Directory.GetDirectories(dataDir))
                {
                    string name = Path.GetFileName(dir);
                    // Match version pattern like "16.0.17932.20842"
                    if (System.Text.RegularExpressions.Regex.IsMatch(name, @"^\d+\.\d+\.\d+\.\d+$"))
                    {
                        officeVersion = name;
                        break;
                    }
                }
            }
        }
        catch { officeVersion = ""; }
    }

    private void InitStrings()
    {
        strTitle = new[] {
            "Office LTSC 2024 离线安装程序",
            "Office LTSC 2024 Offline Installer",
            "Office LTSC 2024 オフライン インストーラー"
        };
        strProduct = new[] {
            "选择要安装的产品：",
            "Select products to install:",
            "インストールする製品を選択："
        };
        strLang = new[] {
            "安装语言 / Language / 言語：",
            "Language / 语言 / 言語：",
            "言語 / Language / 语言："
        };
        strInstall = new[] {
            "开始安装",
            "Install",
            "インストール開始"
        };
        strQR = new[] {
            "关注\"ONE生产力\"公众号",
            "Follow \"ONE Productivity\" Official Account",
            "「ONE生产力」公式アカウントをフォロー"
        };
        strSelectLang = new[] {
            "界面语言 / Interface Language:",
            "Interface Language / 界面语言:",
            "インターフェース言語 / Interface Language:"
        };
        strNoSelect = new[] {
            "请至少选择一个产品！",
            "Please select at least one product!",
            "少なくとも1つの製品を選択してください！"
        };
        strNoProduct = new[] {
            "未找到 Office 安装文件。\n请确保 setup.exe 和 Office 文件夹在同一目录。",
            "Office installation files not found.\nPlease ensure setup.exe and Office folder are in the same directory.",
            "Office インストール ファイルが見つかりません。\nsetup.exe と Office フォルダが同じディレクトリにあることを確認してください。"
        };
        strInstalling = new[] {
            "正在安装 {0} ({1})...",
            "Installing {0} ({1})...",
            "{0} ({1}) をインストール中..."
        };
        strDone = new[] {
            "安装完成！",
            "Installation complete!",
            "インストールが完了しました！"
        };
        strVersion = new[] {
            "Office 版本: ",
            "Office Version: ",
            "Office バージョン: "
        };
    }

    private string T(string[] arr) { return arr[langIndex]; }
    private string PN(int prod) { return productNames[prod][langIndex]; }
    private string LN(int l) { return langNames[langIndex][l]; }

    private void InitUI()
    {
        this.Text = T(strTitle);
        this.Size = new Size(580, 610);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.Font = new Font("Microsoft YaHei", 9F);
        this.BackColor = Color.White;

        // Set form icon
        try
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Office.ico");
            if (File.Exists(iconPath))
                this.Icon = new Icon(iconPath);
        }
        catch { }

        int y = 15;

        // Title
        lblTitle = new Label();
        lblTitle.Text = T(strTitle);
        lblTitle.Location = new Point(20, y);
        lblTitle.Size = new Size(530, 30);
        lblTitle.Font = new Font("Microsoft YaHei", 14F, FontStyle.Bold);
        lblTitle.TextAlign = ContentAlignment.MiddleCenter;
        lblTitle.ForeColor = Color.FromArgb(0, 102, 153);
        this.Controls.Add(lblTitle);
        y += 40;

        // Product selection label
        lblProduct = new Label();
        lblProduct.Text = T(strProduct);
        lblProduct.Location = new Point(25, y);
        lblProduct.Size = new Size(520, 20);
        lblProduct.Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold);
        this.Controls.Add(lblProduct);
        y += 28;

        // Product checkboxes
        productChecks = new CheckBox[productIds.Length];
        for (int i = 0; i < productIds.Length; i++)
        {
            var cb = new CheckBox();
            cb.Text = PN(i);
            cb.Location = new Point(40, y);
            cb.Size = new Size(500, 22);
            cb.Font = new Font("Microsoft YaHei", 9.5F);
            cb.FlatStyle = FlatStyle.Flat;
            productChecks[i] = cb;
            this.Controls.Add(cb);
            y += 26;
        }
        y += 10;

        // Separator
        var sep = new Label();
        sep.Location = new Point(20, y);
        sep.Size = new Size(530, 2);
        sep.BorderStyle = BorderStyle.Fixed3D;
        this.Controls.Add(sep);
        y += 15;

        // Language label
        lblLang = new Label();
        lblLang.Text = T(strLang);
        lblLang.Location = new Point(25, y);
        lblLang.Size = new Size(520, 20);
        lblLang.Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold);
        this.Controls.Add(lblLang);
        y += 28;

        // Language radio buttons
        langRadios = new RadioButton[langCodes.Length];
        int langStartX = 40;
        for (int i = 0; i < langCodes.Length; i++)
        {
            var rb = new RadioButton();
            rb.Text = LN(i);
            rb.Location = new Point(langStartX + i * 160, y);
            rb.Size = new Size(150, 22);
            rb.Font = new Font("Microsoft YaHei", 9.5F);
            rb.FlatStyle = FlatStyle.Flat;
            if (i == langIndex) rb.Checked = true;
            langRadios[i] = rb;
            this.Controls.Add(rb);
        }
        y += 35;

        // Install button
        btnInstall = new Button();
        btnInstall.Text = T(strInstall);
        btnInstall.Location = new Point(150, y);
        btnInstall.Size = new Size(260, 45);
        btnInstall.Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold);
        btnInstall.BackColor = Color.FromArgb(0, 122, 204);
        btnInstall.ForeColor = Color.White;
        btnInstall.FlatStyle = FlatStyle.Flat;
        btnInstall.FlatAppearance.BorderSize = 0;
        btnInstall.Click += BtnInstall_Click;
        this.Controls.Add(btnInstall);
        y += 60;

        // QR code section
        lblQR = new Label();
        lblQR.Text = T(strQR);
        lblQR.Location = new Point(200, y);
        lblQR.Size = new Size(350, 20);
        lblQR.Font = new Font("Microsoft YaHei", 9F);
        lblQR.ForeColor = Color.Gray;
        this.Controls.Add(lblQR);
        y += 22;

        picQR = new PictureBox();
        picQR.Location = new Point(215, y);
        picQR.Size = new Size(130, 130);
        picQR.SizeMode = PictureBoxSizeMode.Zoom;
        picQR.BackColor = Color.White;

        // Load QR image from same directory
        string qrPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "qr.jpg");
        if (File.Exists(qrPath))
        {
            try { picQR.Image = Image.FromFile(qrPath); } catch { }
        }

        this.Controls.Add(picQR);
        y += 138;

        // Version label (centered, between QR and language switcher)
        lblVersion = new Label();
        lblVersion.Text = T(strVersion) + (officeVersion.Length > 0 ? officeVersion : "N/A");
        lblVersion.Location = new Point(20, y);
        lblVersion.Size = new Size(530, 20);
        lblVersion.Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold);
        lblVersion.ForeColor = Color.FromArgb(0, 102, 153);
        lblVersion.TextAlign = ContentAlignment.MiddleCenter;
        this.Controls.Add(lblVersion);
        y += 24;

        // Language switcher at bottom
        var pnlLang = new Panel();
        pnlLang.Location = new Point(0, y);
        pnlLang.Size = new Size(570, 30);

        var lblUILang = new Label();
        lblUILang.Text = T(strSelectLang);
        lblUILang.Location = new Point(25, 5);
        lblUILang.Size = new Size(200, 20);
        lblUILang.Font = new Font("Microsoft YaHei", 9F);
        pnlLang.Controls.Add(lblUILang);

        cmbLang = new ComboBox();
        cmbLang.Location = new Point(230, 3);
        cmbLang.Size = new Size(150, 20);
        cmbLang.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbLang.Font = new Font("Microsoft YaHei", 9F);
        cmbLang.Items.AddRange(new object[] { "中文 (Chinese)", "English", "日本語 (Japanese)" });
        cmbLang.SelectedIndex = langIndex;
        cmbLang.SelectedIndexChanged += OnLangChanged;
        pnlLang.Controls.Add(cmbLang);
        this.Controls.Add(pnlLang);
    }

    private void OnLangChanged(object sender, EventArgs e)
    {
        langIndex = cmbLang.SelectedIndex;
        RefreshUI();
    }

    private void RefreshUI()
    {
        this.Text = T(strTitle);
        lblTitle.Text = T(strTitle);
        lblProduct.Text = T(strProduct);
        lblLang.Text = T(strLang);
        btnInstall.Text = T(strInstall);
        lblQR.Text = T(strQR);
        for (int i = 0; i < productChecks.Length; i++)
            productChecks[i].Text = PN(i);
        for (int i = 0; i < langRadios.Length; i++)
            langRadios[i].Text = LN(i);
        lblVersion.Text = T(strVersion) + (officeVersion.Length > 0 ? officeVersion : "N/A");
    }

    private void BtnInstall_Click(object sender, EventArgs e)
    {
        int selectedLang = -1;
        for (int i = 0; i < langRadios.Length; i++)
        {
            if (langRadios[i].Checked) { selectedLang = i; break; }
        }
        if (selectedLang < 0) selectedLang = 1;

        bool anyProduct = false;
        for (int i = 0; i < productChecks.Length; i++)
        {
            if (productChecks[i].Checked) { anyProduct = true; break; }
        }
        if (!anyProduct)
        {
            MessageBox.Show(T(strNoSelect), "Office LTSC 2024", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string setupExe = Path.Combine(baseDir, "setup.exe");
        if (!File.Exists(setupExe))
        {
            MessageBox.Show(T(strNoProduct), "Office LTSC 2024", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        for (int i = 0; i < productChecks.Length; i++)
        {
            if (!productChecks[i].Checked) continue;

            string productId = productIds[i];
            string langCode = langCodes[selectedLang];
            string msg = string.Format(T(strInstalling), PN(i), LN(selectedLang));

            string configPath = Path.Combine(Path.GetTempPath(),
                "office_install_" + productId + "_" + langCode + ".xml");
            string configXml = "<Configuration>\r\n" +
                "  <Add OfficeClientEdition=\"64\" Channel=\"PerpetualVL2024\">\r\n" +
                "    <Product ID=\"" + productId + "\">\r\n" +
                "      <Language ID=\"" + langCode + "\" />\r\n" +
                "    </Product>\r\n" +
                "  </Add>\r\n" +
                "  <Display Level=\"Full\" AcceptEULA=\"TRUE\" />\r\n" +
                "</Configuration>";
            File.WriteAllText(configPath, configXml);

            try
            {
                var psi = new ProcessStartInfo();
                psi.FileName = setupExe;
                psi.Arguments = "/configure \"" + configPath + "\"";
                psi.UseShellExecute = true;
                psi.WindowStyle = ProcessWindowStyle.Normal;
                var proc = Process.Start(psi);
                if (proc != null)
                {
                    btnInstall.Enabled = false;
                    btnInstall.Text = msg;
                    this.Refresh();
                    proc.WaitForExit();
                    btnInstall.Enabled = true;
                    btnInstall.Text = T(strInstall);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Office LTSC 2024",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        MessageBox.Show(T(strDone), "Office LTSC 2024",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new OfficeInstaller());
    }
}