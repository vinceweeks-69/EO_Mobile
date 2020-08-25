using System;
using System.Runtime.CompilerServices;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace EOMobile
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SchedulerPage : EOBasePage
    {
        DateTime currentMonthDT;

        CalendarMonth calendarMonth;

        Xamarin.Forms.Label currentlySelectedCell = null;

        public SchedulerPage()
        {
            InitializeComponent();

            currentMonthDT = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            Initialize();
        }

        void Initialize()
        {
            calendarMonth = new CalendarMonth(currentMonthDT);

            MonthAndYear.BindingContext = null;

            MonthAndYear.BindingContext = this;

            schedulerCollectionView.ItemsSource = calendarMonth.CalendarWeeks;
        }

        public string GetMonthAndYear { get { return calendarMonth.CalendarWeeks[0].GetMonthAndYear(); } }

        public CalendarMonth Month {get {return calendarMonth;} }

        private void schedulerCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int debug = 1;
        }

        private void TapGestureRecognizer_Tapped(object sender, EventArgs e)
        {
            if(currentlySelectedCell != null)
            {
                currentlySelectedCell.FontSize = 14;
                currentlySelectedCell.TextColor = Color.Black;
            }

            currentlySelectedCell = sender as Xamarin.Forms.Label;

            //highlight the cell?  - popup modal?
            currentlySelectedCell.FontSize = 32;
            currentlySelectedCell.TextColor = Color.Blue;
        }

        private void calendarEarlier_Clicked(object sender, EventArgs e)
        {
            currentMonthDT = currentMonthDT.AddMonths(-1);
            Initialize();
        }

        private void calendarLater_Clicked(object sender, EventArgs e)
        {
            currentMonthDT = currentMonthDT.AddMonths(1);
            Initialize();
        }

        private void Help_SchedulerPage_Clicked(object sender, EventArgs e)
        {
            TaskAwaiter t = Navigation.PushAsync(new HelpPage("SchedulerPage")).GetAwaiter();
        }
    }
}