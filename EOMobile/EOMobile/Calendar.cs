using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOMobile
{
    public class CalendarDay
    {
        public CalendarDay(int year, int month)
        {
            CalendarYear = year;
            CalendarMonth = month;
        }
        public int CalendarYear { get; set; }
        public int CalendarMonth { get; set; }
        public int DayOfMonth { get; set; }
        public string DayText { get { return DayOfMonth != 0 ? DayOfMonth.ToString() : ""; } }
        public Xamarin.Forms.Color BackgroundColor
        {
            get
            {
                Xamarin.Forms.Color dayColor = Xamarin.Forms.Color.White;

                if (DayOfMonth != 0)
                {
                    if (DayOfMonth % 2 != 0)
                    {
                        dayColor = Xamarin.Forms.Color.LightBlue;
                    }
                }

                return dayColor;
            }
        }
    }

    public class CalendarWeek
    {
        DateTime monthDT;
        public CalendarWeek(DateTime monthDT)
        {
            this.monthDT = monthDT;

            calendarDays = new List<CalendarDay>();

            for (int a = 0; a < 7; a++)
            {
                calendarDays.Add(new CalendarDay(monthDT.Year, monthDT.Month));
            }
        }
        List<CalendarDay> calendarDays { get; set; }

        public CalendarDay Sunday { get { return calendarDays[0]; } }

        public CalendarDay Monday { get { return calendarDays[1]; } }

        public CalendarDay Tuesday { get { return calendarDays[2]; } }

        public CalendarDay Wednesday { get { return calendarDays[3]; } }

        public CalendarDay Thursday { get { return calendarDays[4]; } }

        public CalendarDay Friday { get { return calendarDays[5]; } }

        public CalendarDay Saturday { get { return calendarDays[6]; } }

        public void SetDay(int weekIndex, int dayOfMonth)
        {
            calendarDays[weekIndex].DayOfMonth = dayOfMonth;
        }

        public string GetMonthAndYear()
        {
            return monthDT.ToString("MMMM") + " " + monthDT.Year.ToString();
        }
    }

    public class CalendarMonth
    {
        DateTime calendarDate;

        //always pass year / month / DAY = 1
        public CalendarMonth(DateTime monthDT)
        {
            calendarDate = monthDT;

            calendarWeeks = new List<CalendarWeek>();

            for (int a = 0; a < 6; a++)
            {
                calendarWeeks.Add(new CalendarWeek(monthDT));
            }

            //initialize days
            int startDayIndex = (int)(DayOfWeek)monthDT.DayOfWeek;   //Sunday is 0
            bool firstDayDone = false;
            int dayCounter = 1;
            for (int a = 0; a < 6; a++)
            {
                for (int b = 0; b < 7; b++)
                {
                    if (!firstDayDone)
                    {
                        if (b == startDayIndex)
                        {
                            firstDayDone = true;
                        }
                    }

                    if (firstDayDone)
                    {
                        if (dayCounter <= DateTime.DaysInMonth(calendarDate.Year, calendarDate.Month))
                        {
                            calendarWeeks[a].SetDay(b, dayCounter++);
                        }
                    }
                }
            }
        }

        List<CalendarWeek> calendarWeeks { get; set; }

        public List<CalendarWeek> CalendarWeeks { get { return calendarWeeks; } }
    }
}

