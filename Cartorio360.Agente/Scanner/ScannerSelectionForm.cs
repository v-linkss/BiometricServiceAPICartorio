using WIA;

namespace Cartorio360.Agente.Scanner;

/// <summary>
/// Portado do WiaMultiPageScan sem mudanca de comportamento — o atendente ja
/// conhece esta tela.
/// </summary>
public class ScannerSelectionForm : Form
{
    private readonly ListBox _scannerListBox;
    private readonly Button _okButton;
    private readonly Button _cancelButton;

    public Device? SelectedScanner { get; private set; }

    public ScannerSelectionForm()
    {
        Text = "Selecionar Scanner";
        Width = 400;
        Height = 300;
        Icon = Recursos.CarregarGrande();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;

        _scannerListBox = new ListBox { Dock = DockStyle.Top, Height = 200 };
        Controls.Add(_scannerListBox);

        var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 50 };
        _okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Dock = DockStyle.Right, Width = 80 };
        _cancelButton = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Dock = DockStyle.Right, Width = 80 };
        buttonPanel.Controls.Add(_cancelButton);
        buttonPanel.Controls.Add(_okButton);
        Controls.Add(buttonPanel);

        CarregarScanners();

        _okButton.Click += OkButton_Click;
    }

    private void CarregarScanners()
    {
        try
        {
            var deviceManager = new DeviceManager();
            for (int i = 1; i <= deviceManager.DeviceInfos.Count; i++)
            {
                var deviceInfo = deviceManager.DeviceInfos[i];
                if (deviceInfo.Type == WiaDeviceType.ScannerDeviceType)
                    _scannerListBox.Items.Add(new ScannerItem(deviceInfo));
            }
        }
        catch (Exception ex)
        {
            Log.Erro("falha ao enumerar scanners WIA", ex);
        }

        if (_scannerListBox.Items.Count > 0)
            _scannerListBox.SelectedIndex = 0;
    }

    private void OkButton_Click(object? sender, EventArgs e)
    {
        SelectedScanner = _scannerListBox.SelectedItem is ScannerItem item
            ? item.DeviceInfo.Connect()
            : null;
    }

    private sealed class ScannerItem
    {
        public DeviceInfo DeviceInfo { get; }
        public ScannerItem(DeviceInfo deviceInfo) => DeviceInfo = deviceInfo;
        public override string ToString()
        {
            try { return DeviceInfo.Properties["Name"].get_Value()?.ToString() ?? "(sem nome)"; }
            catch { return "(scanner)"; }
        }
    }
}
