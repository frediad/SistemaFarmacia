using FarmaciaPOS.Helpers;
using FarmaciaPOS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Windows;

namespace FarmaciaPOS.Views
{
    public partial class ProveedoresWindow : Window
    {
        

        List<Proveedor> proveedores =
            new();

        public ProveedoresWindow()
        {
            InitializeComponent();

            CargarProveedores();
        }

        // =====================================
        // CARGAR
        // =====================================

        private void CargarProveedores()
        {
            proveedores.Clear();

            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT *
              FROM Proveedores";

            SqlCommand cmd =
                new SqlCommand(query, conn);

            SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                proveedores.Add(new Proveedor
                {
                    Id =
                        Convert.ToInt32(reader["Id"]),

                    Nombre =
                        reader["Nombre"].ToString(),

                    Telefono =
                        reader["Telefono"].ToString(),

                    Correo =
                        reader["Correo"].ToString(),

                    Direccion =
                        reader["Direccion"].ToString(),

                    Contacto =
                        reader["Contacto"].ToString()
                });
            }

            dgProveedores.ItemsSource = null;
            dgProveedores.ItemsSource = proveedores;
        }

        // =====================================
        // AGREGAR
        // =====================================

        private void BtnAgregar_Click(
            object sender,
            RoutedEventArgs e)
        {
            string nombre =
                Microsoft.VisualBasic.Interaction
                .InputBox("Nombre:");

            if (string.IsNullOrEmpty(nombre))
                return;

            string telefono =
                Microsoft.VisualBasic.Interaction
                .InputBox("Teléfono:");

            string correo =
                Microsoft.VisualBasic.Interaction
                .InputBox("Correo:");

            string direccion =
                Microsoft.VisualBasic.Interaction
                .InputBox("Dirección:");

            string contacto =
                Microsoft.VisualBasic.Interaction
                .InputBox("Contacto:");

            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"INSERT INTO Proveedores
            (
                Nombre,
                Telefono,
                Correo,
                Direccion,
                Contacto
            )
            VALUES
            (
                @Nombre,
                @Telefono,
                @Correo,
                @Direccion,
                @Contacto
            )";

            SqlCommand cmd =
                new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue(
                "@Nombre",
                nombre);

            cmd.Parameters.AddWithValue(
                "@Telefono",
                telefono);

            cmd.Parameters.AddWithValue(
                "@Correo",
                correo);

            cmd.Parameters.AddWithValue(
                "@Direccion",
                direccion);

            cmd.Parameters.AddWithValue(
                "@Contacto",
                contacto);

            cmd.ExecuteNonQuery();

            MessageBox.Show(
                "Proveedor agregado");

            CargarProveedores();
        }

        // =====================================
        // ELIMINAR
        // =====================================

        private void BtnEliminar_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (dgProveedores.SelectedItem
                is not Proveedor proveedor)
            {
                MessageBox.Show(
                    "Selecciona un proveedor");

                return;
            }

            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"DELETE FROM Proveedores
              WHERE Id = @Id";

            SqlCommand cmd =
                new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue(
                "@Id",
                proveedor.Id);

            cmd.ExecuteNonQuery();

            MessageBox.Show(
                "Proveedor eliminado");

            CargarProveedores();
        }

        private void BtnPedirMercancia_Click(object sender, RoutedEventArgs e)
        {
            if (dgProveedores.SelectedItem is not Proveedor proveedor)
            {
                MessageBox.Show("Selecciona un proveedor de la lista");
                return;
            }

            var ventana = new PedirMercanciaWindow(proveedor)
            {
                Owner = this
            };

            ventana.ShowDialog();
        }
    }
}