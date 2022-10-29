using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Vishnu.WPF_UI.DependencyProperties
{
    /// <summary>
    /// Diese Klasse stellt DependencyProperties für die Zeilenzahl und Spaltenzahl
    /// von WPF-Grids zur Verfügung. Damit wird es möglich, die Spalten- und Zeilenzahl
    /// von Grids zur Laufzeit über Databinding zu setzen.
    /// Ich habe die Logik nahezu unverändert von Rachel Lim's Blog auf
    /// https://rachel53461.wordpress.com/2011/09/17/wpf-grids-rowcolumn-count-properties/
    /// übernommen.
    /// Thanks and respect to Rachel Lim on https://rachel53461.wordpress.com/
    /// </summary>
    /// <remarks>
    /// 03.10.2022 Erik Nagel: created.
    /// </remarks>
    public static class GridHelpers
    {
        #region RowCount Property

        /// <summary>
        /// Adds the specified number of Rows to RowDefinitions. 
        /// Default Height is Auto.
        /// </summary>
        public static readonly DependencyProperty RowCountProperty =
            DependencyProperty.RegisterAttached(
                "RowCount", typeof(int), typeof(GridHelpers),
                new PropertyMetadata(-1, RowCountChanged));

        /// <summary>
        /// Returns the actual RowCout of a given Grid.
        /// </summary>
        /// <param name="obj">Grid as DependencyObject.</param>
        /// <returns>Actual RowCout of the given Grid.</returns>
        public static int GetRowCount(DependencyObject obj)
        {
            return (int)obj.GetValue(RowCountProperty);
        }

        /// <summary>
        /// Sets the RowCout of a given Grid to the given value.
        /// </summary>
        /// <param name="obj">Grid as DependencyObject.</param>
        /// <param name="value">New RowCout for the given Grid.</param>
        public static void SetRowCount(DependencyObject obj, int value)
        {
            obj.SetValue(RowCountProperty, value);
        }

        /// <summary>
        /// Change Event - Adds the Rows.
        /// </summary>
        /// <param name="obj">Grid as DependencyObject.</param>
        /// <param name="e">DependencyPropertyChangedEventArgs containing the new row count.</param>
        public static void RowCountChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is Grid) || (int)e.NewValue < 0)
                return;

            Grid grid = (Grid)obj;
            grid.RowDefinitions.Clear();

            for (int i = 0; i < (int)e.NewValue; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            }

            SetRowsProperties(grid, GridHelpers._gridRowUnitType);
        }

        #endregion

        #region ColumnCount Property

        /// <summary>
        /// Adds the specified number of Columns to ColumnDefinitions. 
        /// Default Width is Auto.
        /// </summary>
        public static readonly DependencyProperty ColumnCountProperty =
            DependencyProperty.RegisterAttached(
                "ColumnCount", typeof(int), typeof(GridHelpers),
                new PropertyMetadata(-1, ColumnCountChanged));

        /// <summary>
        /// Returns the actual ColumnCout of a given Grid.
        /// </summary>
        /// <param name="obj">Grid as DependencyObject.</param>
        /// <returns>Actual ColumnCout of the given Grid.</returns>
        public static int GetColumnCount(DependencyObject obj)
        {
            return (int)obj.GetValue(ColumnCountProperty);
        }

        /// <summary>
        /// Sets the ColumnCout of a given Grid to the given value.
        /// </summary>
        /// <param name="obj">Grid as DependencyObject.</param>
        /// <param name="value">New ColumnCout for the given Grid.</param>
        public static void SetColumnCount(DependencyObject obj, int value)
        {
            obj.SetValue(ColumnCountProperty, value);
        }

        /// <summary>
        /// Change Event - Adds the columns.
        /// </summary>
        /// <param name="obj">Grid as DependencyObject.</param>
        /// <param name="e">DependencyPropertyChangedEventArgs containing the new column count.</param>
        public static void ColumnCountChanged(
            DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is Grid) || (int)e.NewValue < 0)
                return;

            Grid grid = (Grid)obj;
            grid.ColumnDefinitions.Clear();

            for (int i = 0; i < (int)e.NewValue; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            }

            SetColumnsProperties(grid, GridHelpers._gridColumnUnitType);
        }

        #endregion

        #region RowsProperties Property

        /// <summary>
        /// Makes the specified Row's Height equal to Auto or Star depending on GridHelpers._gridRowUnitType. 
        /// Can set on multiple Rows.
        /// </summary>
        public static readonly DependencyProperty RowsPropertiesProperty =
            DependencyProperty.RegisterAttached(
                "RowsProperties", typeof(string), typeof(GridHelpers),
                new PropertyMetadata(string.Empty, RowsPropertiesChanged));

        /// <summary>
        /// Returns a string representing the value of the RowsPropertiesProperty of a given Grid.
        /// </summary>
        /// <param name="obj">Grid as DependencyObject.</param>
        /// <returns>A string representing the value of the RowsPropertiesProperty of a given Grid.</returns>
        public static string GetRowsProperties(DependencyObject obj)
        {
            return (string)obj.GetValue(RowsPropertiesProperty);
        }

        /// <summary>
        /// Sets the new value of the RowsPropertiesProperty of a given Grid.
        /// </summary>
        /// <param name="obj">Grid as DependencyObject.</param>
        /// <param name="value">A string representing the value of the RowsPropertiesProperty of a given Grid.</param>
        public static void SetRowsProperties(DependencyObject obj, string value)
        {
            obj.SetValue(RowsPropertiesProperty, value);
        }

        /// <summary>
        /// Change Event - Makes the specified Row's Height equal to Auto or Star depending on GridHelpers._gridRowUnitType. 
        /// </summary>
        /// <param name="obj">Grid as DependencyObject.</param>
        /// <param name="e">DependencyPropertyChangedEventArgs containing the new RowsPropertiesProperty as string.</param>
        public static void RowsPropertiesChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is Grid) || string.IsNullOrEmpty(e.NewValue?.ToString()))
                return;

            SetRowsProperties((Grid)obj, GridHelpers._gridColumnUnitType);
        }

        #endregion

        #region StarColumns Property

        /// <summary>
        /// Makes the specified Column's Width equal to Auto or Star depending on GridHelpers._gridColumnUnitType.
        /// Can set on multiple Columns.
        /// </summary>
        public static readonly DependencyProperty ColumnsPropertiesProperty =
            DependencyProperty.RegisterAttached(
                "ColumnsProperties", typeof(string), typeof(GridHelpers),
                new PropertyMetadata(string.Empty, ColumnsPropertiesChanged));

        /// <summary>
        /// Returns a string representing the value of the ColumnsPropertiesProperty of a given Grid.
        /// </summary>
        /// <param name="obj">Grid as DependencyObject.</param>
        /// <returns>A string representing the value of the ColumnsPropertiesProperty of a given Grid.</returns>
        public static string GetColumnsProperties(DependencyObject obj)
        {
            return (string)obj.GetValue(ColumnsPropertiesProperty);
        }

        /// <summary>
        /// Sets the new value of the ColumnsPropertiesProperty of a given Grid.
        /// </summary>
        /// <param name="obj">Grid as DependencyObject.</param>
        /// <param name="value">A string representing the value of the ColumnsPropertiesProperty of a given Grid.</param>
        public static void SetColumnsProperties(DependencyObject obj, string value)
        {
            obj.SetValue(ColumnsPropertiesProperty, value);
        }

        /// <summary>
        /// Change Event - Makes specified Column's Width equal to Auto or Star depending on GridHelpers._gridColumnUnitType.
        /// </summary>
        /// <param name="obj">Grid as DependencyObject.</param>
        /// <param name="e">DependencyPropertyChangedEventArgs containing the new ColumnsPropertiesProperty as string.</param>
        public static void ColumnsPropertiesChanged(
            DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is Grid) || string.IsNullOrEmpty(e.NewValue?.ToString()))
                return;

            SetColumnsProperties((Grid)obj, GridHelpers._gridColumnUnitType);
        }

        #endregion

        private static GridUnitType _gridColumnUnitType;
        private static GridUnitType _gridRowUnitType;

        static GridHelpers()
        {
            _gridColumnUnitType = GridUnitType.Auto;
            _gridRowUnitType = GridUnitType.Auto;
        }

        private static void SetColumnsProperties(Grid grid, GridUnitType starOrAuto)
        {
            string[] starColumns;

            if (string.IsNullOrWhiteSpace(GetColumnsProperties(grid)))
            {
                starColumns = new string[grid.ColumnDefinitions.Count];
                for (var i = 0; i < grid.ColumnDefinitions.Count; i++)
                    starColumns[i] = i.ToString();
            }
            else
            {
                starColumns = GetColumnsProperties(grid).Split(',');
            }

            for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
            {
                if (starColumns.Contains(i.ToString()))
                {
                    grid.ColumnDefinitions[i].Width = new GridLength(1, starOrAuto);
                }
            }
        }

        private static void SetRowsProperties(Grid grid, GridUnitType starOrAuto)
        {
            string[] starRows;

            if (string.IsNullOrWhiteSpace(GetRowsProperties(grid)))
            {
                starRows = new string[grid.RowDefinitions.Count];
                for (var i = 0; i < grid.RowDefinitions.Count; i++)
                    starRows[i] = i.ToString();
            }
            else
            {
                starRows = GetRowsProperties(grid).Split(',');
            }

            for (int i = 0; i < grid.RowDefinitions.Count; i++)
            {
                if (starRows.Contains(i.ToString()))
                {
                    grid.RowDefinitions[i].Height = new GridLength(1, starOrAuto);
                }
            }
        }
    }
}