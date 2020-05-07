using System;
using System.Text;
using System.Linq;
using RedCorners;
using RedCorners.Forms;
using RedCorners.Models;
using System.Collections.Generic;
using Xamarin.Forms;

namespace RedCorners.Forms.ZXing.Demo.ViewModels
{
    public class MainViewModel : BindableModel
    {
        bool _isTorchOn = false;
        public bool IsTorchOn
        {
            get => _isTorchOn;
            set => SetProperty(ref _isTorchOn, value);
        }

        public MainViewModel()
        {
            Status = TaskStatuses.Success;
        }

    }
}