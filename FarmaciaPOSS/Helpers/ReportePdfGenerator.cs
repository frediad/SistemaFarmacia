using FarmaciaPOS.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;

namespace FarmaciaPOS.Helpers
{
    public static class ReportePdfGenerator
    {
        // =========================================
        // REPORTE DE VENTAS 
        // =========================================

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

        // =========================================
        // REPORTE DE (Más Vendidos, Ganancias, Inventario, Pedidos, etc.)
        // =========================================

        public static void GenerarReporteGenerico(
            string tituloReporte,
            string periodo,
            DateTime desde,
            DateTime hasta,
            List<(string Etiqueta, string Valor)> tarjetasResumen,
            List<string> encabezados,
            List<List<string>> filas,
            string rutaArchivo,
            List<(string Etiqueta, string Valor)>? tablaSecundariaResumen = null,
            string? tituloTablaSecundaria = null,
            List<string>? encabezadosSecundarios = null,
            List<List<string>>? filasSecundarias = null)
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

                        col.Item().Text($"{tituloReporte} — {periodo}")
                            .FontSize(14)
                            .FontColor(Colors.Grey.Darken1);

                        col.Item().Text($"Del {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Medium);

                        col.Item().PaddingTop(10).LineHorizontal(1)
                            .LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        // TARJETAS DE RESUMEN (si se proporcionan)
                        if (tarjetasResumen != null && tarjetasResumen.Count > 0)
                        {
                            col.Item().Row(row =>
                            {
                                bool primero = true;

                                foreach (var tarjeta in tarjetasResumen)
                                {
                                    if (!primero)
                                        row.ConstantItem(10);

                                    row.RelativeItem()
                                        .Background(Colors.Blue.Lighten4)
                                        .Padding(12)
                                        .Column(c =>
                                        {
                                            c.Item().Text(tarjeta.Etiqueta)
                                                .FontSize(11).FontColor(Colors.Grey.Darken2);
                                            c.Item().Text(tarjeta.Valor)
                                                .FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                                        });

                                    primero = false;
                                }
                            });

                            col.Item().PaddingTop(15);
                        }

                        // TABLA PRINCIPAL
                        if (encabezados != null && encabezados.Count > 0)
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    foreach (var _ in encabezados)
                                        columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    foreach (var encabezado in encabezados)
                                    {
                                        header.Cell().Background(Colors.Blue.Darken2)
                                            .Padding(8).Text(encabezado)
                                            .FontColor(Colors.White).Bold();
                                    }
                                });

                                bool alterno = false;

                                foreach (var fila in filas)
                                {
                                    var bgColor = alterno ? Colors.Grey.Lighten4 : Colors.White;

                                    foreach (var celda in fila)
                                    {
                                        table.Cell().Background(bgColor)
                                            .Padding(8).Text(celda ?? "");
                                    }

                                    alterno = !alterno;
                                }
                            });
                        }

                        // SEGUNDA SECCIÓN (opcional — ej. "Productos sin movimiento" o "Alertas de stock")
                        if (encabezadosSecundarios != null && filasSecundarias != null && filasSecundarias.Count > 0)
                        {
                            col.Item().PaddingTop(20).Text(tituloTablaSecundaria ?? "")
                                .FontSize(14).Bold();

                            col.Item().PaddingTop(8).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    foreach (var _ in encabezadosSecundarios)
                                        columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    foreach (var encabezado in encabezadosSecundarios)
                                    {
                                        header.Cell().Background(Colors.Grey.Darken2)
                                            .Padding(8).Text(encabezado)
                                            .FontColor(Colors.White).Bold();
                                    }
                                });

                                bool alterno = false;

                                foreach (var fila in filasSecundarias)
                                {
                                    var bgColor = alterno ? Colors.Grey.Lighten4 : Colors.White;

                                    foreach (var celda in fila)
                                    {
                                        table.Cell().Background(bgColor)
                                            .Padding(8).Text(celda ?? "");
                                    }

                                    alterno = !alterno;
                                }
                            });
                        }
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