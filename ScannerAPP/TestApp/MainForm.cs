using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TwainDotNet;
using System.IO;
using TwainDotNet.WinFroms;

namespace TestApp
{
    using TwainDotNet.TwainNative;

    public partial class MainForm : Form
    {
        private static AreaSettings AreaSettings = new AreaSettings(Units.Centimeters, 0.1f, 5.7f, 0.1F + 2.6f, 5.7f + 2.6f);

        Twain _twain;
        ScanSettings _settings;

        public MainForm()
        {
            InitializeComponent();

            _twain = new Twain(new WinFormsWindowMessageHook(this));
            _twain.TransferImage += delegate (Object sender, TransferImageEventArgs args)
            {
                if (args.Image != null)
                {
                    // Limites máximos
                    const int maxWidth = 1980;
                    const int maxHeight = 1080;

                    // Calculando a nova largura e altura mantendo a proporção
                    int newWidth = args.Image.Width;
                    int newHeight = args.Image.Height;

                    if (newWidth > maxWidth || newHeight > maxHeight)
                    {
                        float widthRatio = (float)maxWidth / newWidth;
                        float heightRatio = (float)maxHeight / newHeight;
                        float ratio = Math.Min(widthRatio, heightRatio);

                        newWidth = (int)(newWidth * ratio);
                        newHeight = (int)(newHeight * ratio);
                    }

                    // Redimensionando a imagem
                    var resizedImage = new Bitmap(args.Image, new Size(newWidth, newHeight));

                    // Atualizando o PictureBox e as labels
                    pictureBox1.Image = resizedImage;

                    widthLabel.Text = "Width: " + resizedImage.Width;
                    heightLabel.Text = "Height: " + resizedImage.Height;
                }
            };

            _twain.ScanningComplete += delegate
            {
                Enabled = true;
            };
        }

        private void selectSource_Click(object sender, EventArgs e)
        {
            _twain.SelectSource();
        }

        private void scan_Click(object sender, EventArgs e)
        {
            Enabled = false;

            _settings = new ScanSettings();
            _settings.UseDocumentFeeder = useAdfCheckBox.Checked;
            _settings.ShowTwainUI = useUICheckBox.Checked;
            _settings.ShowProgressIndicatorUI = showProgressIndicatorUICheckBox.Checked;
            _settings.UseDuplex = useDuplexCheckBox.Checked;
            _settings.Area = !checkBoxArea.Checked ? null : AreaSettings;
            _settings.ShouldTransferAllPages = true;

            _settings.Rotation = new RotationSettings()
            {
                AutomaticRotate = autoRotateCheckBox.Checked,
                AutomaticBorderDetection = autoDetectBorderCheckBox.Checked
            };

            try
            {
                _twain.StartScanning(_settings);
            }
            catch (TwainException ex)
            {
                MessageBox.Show(ex.Message);
                Enabled = true;
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    // Configurando o filtro para tipos de arquivos
                    sfd.Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|Bitmap Image|*.bmp|All Files|*.*";
                    sfd.Title = "Save an Image File";
                    sfd.DefaultExt = "png"; // Define a extensão padrão como PNG
                    sfd.AddExtension = true; // Adiciona automaticamente a extensão ao salvar

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        // Salvar a imagem de acordo com o formato escolhido
                        switch (Path.GetExtension(sfd.FileName).ToLower())
                        {
                            case ".png":
                                pictureBox1.Image.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
                                break;
                            case ".jpg":
                            case ".jpeg":
                                pictureBox1.Image.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                                break;
                            case ".bmp":
                                pictureBox1.Image.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Bmp);
                                break;
                            default:
                                // Se não for um formato suportado, você pode adicionar um aviso ou tratamento de erro
                                MessageBox.Show("Formato de arquivo não suportado.");
                                break;
                        }
                    }
                }
            }
        }


        private void diagnostics_Click(object sender, EventArgs e)
        {
            var diagnostics = new Diagnostics(new WinFormsWindowMessageHook(this));
        }
    }
}
