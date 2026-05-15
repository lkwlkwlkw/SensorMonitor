using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;

namespace SensorMonitor
{
    public static class OxyPlotCloner
    {
        public static PlotModel CloneModel(PlotModel original)
        {
            var clone = new PlotModel
            {
                Title = original.Title,
                Subtitle = original.Subtitle,
                Background = original.Background,
                PlotAreaBorderColor = original.PlotAreaBorderColor,
                PlotAreaBorderThickness = original.PlotAreaBorderThickness,
                IsLegendVisible = original.IsLegendVisible,
                Legends = { new Legend { LegendPosition = LegendPosition.TopRight } }

            };

            // --- Osie ---
            foreach (var axis in original.Axes)
                clone.Axes.Add(CloneAxis(axis));

            // --- Serie ---
            foreach (var series in original.Series)
                clone.Series.Add(CloneSeries(series));

            return clone;
        }

        // -------------------------
        //   KLONOWANIE SERII
        // -------------------------
        private static Series CloneSeries(Series s)
        {
            switch (s)
            {
                case LineSeries ls:

                    return CloneLineSeries(ls);

                case ScatterSeries ss:
                    return CloneScatterSeries(ss);



                case BarSeries bs:
                    return CloneBarSeries(bs);

                default:
                    throw new NotSupportedException(
                        $"Klonowanie typu serii {s.GetType().Name} nie jest obsługiwane.");
            }
        }

        private static LineSeries CloneLineSeries(LineSeries original)
        {
            var copy = new LineSeries
            {
                Title = original.Title,
                Color = original.Color,
                StrokeThickness = original.StrokeThickness,
                LineStyle = original.LineStyle,
                MarkerType = original.MarkerType,
                MarkerSize = original.MarkerSize,
                MarkerFill = original.MarkerFill
            };

            foreach (var p in original.Points)
                copy.Points.Add(new DataPoint(p.X, p.Y));

            return copy;
        }


        private static ScatterSeries CloneScatterSeries(ScatterSeries original)
        {
            var copy = new ScatterSeries
            {
                Title = original.Title,
                MarkerType = original.MarkerType,
                MarkerFill = original.MarkerFill,
                MarkerSize = original.MarkerSize
            };

            foreach (var p in original.Points)
                copy.Points.Add(new ScatterPoint(p.X, p.Y, p.Size, p.Value));

            return copy;
        }

        private static AreaSeries CloneAreaSeries(AreaSeries original)
        {
            var copy = new AreaSeries
            {
                Title = original.Title,
                Color = original.Color,
                Color2 = original.Color2,
                StrokeThickness = original.StrokeThickness
            };

            foreach (var p in original.Points)
                copy.Points.Add(new DataPoint(p.X, p.Y));

            foreach (var p in original.Points2)
                copy.Points2.Add(new DataPoint(p.X, p.Y));

            return copy;
        }

        private static BarSeries CloneBarSeries(BarSeries original)
        {
            var copy = new BarSeries
            {
                Title = original.Title,
                FillColor = original.FillColor,
                StrokeColor = original.StrokeColor,
                StrokeThickness = original.StrokeThickness
            };

            foreach (var item in original.Items)
                copy.Items.Add(new BarItem(item.Value));

            return copy;
        }

        // -------------------------
        //   KLONOWANIE OSI
        // -------------------------
        private static Axis CloneAxis(Axis original)
        {
            var copy = (Axis)Activator.CreateInstance(original.GetType());

            copy.Position = original.Position;
            copy.Title = original.Title;
            copy.Minimum = original.Minimum;
            copy.Maximum = original.Maximum;
            copy.MajorGridlineStyle = original.MajorGridlineStyle;
            copy.MinorGridlineStyle = original.MinorGridlineStyle;
            copy.StringFormat = original.StringFormat;
            copy.IsZoomEnabled = original.IsZoomEnabled;
            copy.IsPanEnabled = original.IsPanEnabled;
            return copy;
        }
    }




}
