using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;

namespace FarmaciaPOS.Models
{
    public class TicketPrinter
    {
        private List<VentaItem> carrito;

        private decimal total;

        public TicketPrinter(
            List<VentaItem> carrito,
            decimal total)
        {
            this.carrito = carrito;

            this.total = total;
        }

        public void Imprimir()
        {
            FlowDocument doc =
                new FlowDocument();

            doc.PageWidth = 300;

            doc.FontFamily =
                new System.Windows.Media.FontFamily(
                    "Arial");

            doc.FontSize = 12;

            // =====================================
            // TITULO
            // =====================================

            Paragraph titulo =
                new Paragraph(
                    new Run("Farmaclick Yatzil"));

            titulo.FontSize = 18;

            titulo.FontWeight =
                FontWeights.Bold;

            titulo.TextAlignment =
                TextAlignment.Center;

            doc.Blocks.Add(titulo);

            // =====================================
            // FECHA
            // =====================================

            Paragraph fecha =
                new Paragraph(
                    new Run(
                        DateTime.Now.ToString()));

            fecha.TextAlignment =
                TextAlignment.Center;

            doc.Blocks.Add(fecha);

            // =====================================
            // PRODUCTOS
            // =====================================

            foreach (var item in carrito)
            {
                Paragraph producto =
                    new Paragraph();

                producto.Inlines.Add(
                    new Run(
                        item.Nombre));

                producto.Inlines.Add(
                    new Run(
                    $"\n{item.Cantidad} x {item.Precio:C}"));

                producto.Inlines.Add(
                    new Run(
                    $" = {item.Subtotal:C}"));

                doc.Blocks.Add(producto);
            }

            // =====================================
            // TOTAL
            // =====================================

            Paragraph totalText =
                new Paragraph(
                    new Run(
                        $"TOTAL: {total:C}"));

            totalText.FontSize = 20;

            totalText.FontWeight =
                FontWeights.Bold;

            totalText.TextAlignment =
                TextAlignment.Center;

            doc.Blocks.Add(totalText);

            // =====================================
            // GRACIAS
            // =====================================

            Paragraph gracias =
                new Paragraph(
                    new Run(
                        "Gracias por su compra"));

            gracias.TextAlignment =
                TextAlignment.Center;

            doc.Blocks.Add(gracias);

            // =====================================
            // IMPRIMIR
            // =====================================

            PrintDialog pd =
                new PrintDialog();

            pd.PrintDocument(
                ((IDocumentPaginatorSource)doc)
                .DocumentPaginator,

                "Ticket");
        }
    }
}