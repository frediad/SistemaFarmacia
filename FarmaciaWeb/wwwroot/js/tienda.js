async function comprar(idProducto) {
    const datos =
    {
        productoId: idProducto,

        cantidad:
            parseInt(
                document.getElementById("cantidad").value),

        nombreCliente:
            document.getElementById("nombre").value,

        telefono:
            document.getElementById("telefono").value,

        correo:
            document.getElementById("correo").value,

        direccion:
            document.getElementById("direccion").value
    };

    const respuesta =
        await fetch(
            "https://localhost:7106/api/ventas/comprar",
            {
                method: "POST",

                headers:
                {
                    "Content-Type":
                        "application/json"
                },

                body:
                    JSON.stringify(datos)
            });

    const resultado =
        await respuesta.json();

    alert(resultado.mensaje);
}