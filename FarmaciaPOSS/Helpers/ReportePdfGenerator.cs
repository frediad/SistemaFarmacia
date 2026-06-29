using FarmaciaPOS.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;

namespace FarmaciaPOS.Helpers
{
    public static class ReportePdfGenerator
    {
        public static void GenerarReporte(
            ReporteResumen resumen,
            string rutaArchivo,
            string periodo)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("FarmaClick Yatzil")
                            .FontSize(22)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);

                        col.Item().Text($"Reporte de Ventas — {periodo}")
                            .FontSize(14)
                            .FontColor(Colors.Grey.Darken1);

                        col.Item().Text(
                            $"Del {resumen.FechaInicio:dd/MM/yyyy} al {resumen.FechaFin:dd/MM/yyyy}")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Medium);

                        col.Item().PaddingTop(10).LineHorizontal(1)
                            .LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        // RESUMEN
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Background(Colors.Blue.Lighten4)
                                .Padding(15).Column(c =>
                                {
                                    c.Item().Text("Total Vendido")
                                        .FontSize(12).FontColor(Colors.Grey.Darken2);
                                    c.Item().Text(resumen.TotalVentas.ToString("C"))
                                        .FontSize(24).Bold().FontColor(Colors.Green.Darken2);
                                });

                            row.ConstantItem(15);

                            row.RelativeItem().Background(Colors.Grey.Lighten3)
                                .Padding(15).Column(c =>
                                {
                                    c.Item().Text("Número de Ventas")
                                        .FontSize(12).FontColor(Colors.Grey.Darken2);
                                    c.Item().Text(resumen.NumeroVentas.ToString())
                                        .FontSize(24).Bold();
                                });
                        });

                        col.Item().PaddingTop(20).Text("Productos Vendidos")
                            .FontSize(16).Bold();

                        // TABLA
                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken2)
                                    .Padding(8).Text("Producto")
                                    .FontColor(Colors.White).Bold();

                                header.Cell().Background(Colors.Blue.Darken2)
                                    .Padding(8).Text("Cantidad")
                                    .FontColor(Colors.White).Bold();

                                header.Cell().Background(Colors.Blue.Darken2)
                                    .Padding(8).Text("Total")
                                    .FontColor(Colors.White).Bold();
                            });

                            bool alterno = false;

                            foreach (var item in resumen.Productos)
                            {
                                var bgColor = alterno ? Colors.Grey.Lighten4 : Colors.White;

                                table.Cell().Background(bgColor)
                                    .Padding(8).Text(item.Producto);

                                table.Cell().Background(bgColor)
                                    .Padding(8).Text(item.Cantidad.ToString());

                                table.Cell().Background(bgColor)
                                    .Padding(8).Text(item.Total.ToString("C"));

                                alterno = !alterno;
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Generado el ");
                        text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).Bold();
                    });
                });
            })
            .GeneratePdf(rutaArchivo);
        }
    }
}