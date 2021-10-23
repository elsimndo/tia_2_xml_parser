using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace S7SourceToXmlUI {
    public class MainWindowViewModel : ViewModelBase{
        readonly BackgroundWorker _workerThread;

        public MainWindowViewModel() {

            ChooseSource = new DelegateCommand(OnChooseSourceExecuted);
            ChooseDest = new DelegateCommand(OnChooseDestExecuted);
            GenerateXml = new DelegateCommand(OnGenerateXmlExecuted);

            pbMaximum = 0;
            pbValue = 0;

            _workerThread = new BackgroundWorker {WorkerSupportsCancellation = false};
            _workerThread.DoWork += WorkerThread_DoWork;

        }


        private void WorkerThread_DoWork(object sender, DoWorkEventArgs e) {
            XmlFileGenerator.ReadDataBlockFile(this, tbSource, tbDest);
        }

        public ICommand ChooseSource { get; set; }
        public ICommand ChooseDest { get; set; }
        public ICommand GenerateXml { get; set; }


        private string _tbSource;
        public string tbSource {
            get { return _tbSource; }
            set {
                _tbSource = value;
                OnPropertyChanged("tbSource");
            }
        }


        private string _tbDest;
        public string tbDest {
            get { return _tbDest; }
            set {
                _tbDest = value;
                OnPropertyChanged("tbDest");
            }
        }


        private string _tbBezeichnung;
        public string tbBezeichnung {
            get {
                if (cbStoerungChecked)
                    return "ERROR";
                if (cbFehlerChecked)
                    return "WARNING";
                if (cbHinweisChecked)
                    return "INFO";
                else
                    return _tbBezeichnung.ToUpper(); }
            set { _tbBezeichnung = value; }
        }


        private bool _cbStoerungChecked;
        public bool cbStoerungChecked {
            get { return _cbStoerungChecked; }
            set { _cbStoerungChecked = value; }
        }


        private bool _cbFehlerChecked;
        public bool cbFehlerChecked {
            get { return _cbFehlerChecked; }
            set { _cbFehlerChecked = value; }
        }


        private bool _cbHinweisChecked;
        public bool cbHinweisChecked {
            get { return _cbHinweisChecked; }
            set { _cbHinweisChecked = value; }
        }


        private string _tbAusgabe;
        public string tbAusgabe {
            get { return _tbAusgabe; }
            set {
                _tbAusgabe = value;
                OnPropertyChanged("tbAusgabe");
            }
        }


        private string _tbDBName;
        public string tbDBName {
            get { return _tbDBName; }
            set {
                _tbDBName = value;
                OnPropertyChanged("tbDBName");
            }
        }

        private string _tblProgress;
        public string tblProgress {
            get { return _tblProgress; }
            set {
                _tblProgress = value;
                OnPropertyChanged("tblProgress");
            }
        }


        private int _pbValue;
        public int pbValue {
            get { return _pbValue * 100 / pbMaximum; }
            set { _pbValue = value;
                OnPropertyChanged("pbValue");
            }
        }


        private int _pbMaximum;
        public int pbMaximum {
            get { return _pbMaximum; }
            set { _pbMaximum = value;
                OnPropertyChanged("pbMaximum");
            }
        }


        private void OnChooseSourceExecuted(object param) {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "DB files (*.db)|*.db|All files (*.*)|*.*";
            dialog.DefaultExt = "db";

            if (dialog.ShowDialog() == true) {
                tbSource = dialog.FileName;
            }
        }

        private void OnChooseDestExecuted(object param) {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog()) {

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    tbDest = dialog.SelectedPath + @"\";
                }
            }
        }

        private void OnGenerateXmlExecuted(object param) {
            tbAusgabe = "";
            MessageBoxResult result;
            bool doit = false;

            if (tbBezeichnung == null || tbBezeichnung == "") {
                result = MessageBox.Show("Keinen Typ gewählt", "Warnung", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.OK);

                if (result == MessageBoxResult.OK) {
                    doit = true;
                }
            }
            else {
                doit = true;
            }

            if (doit) {
                    _workerThread.RunWorkerAsync();
            }
                
        }
    }
}
