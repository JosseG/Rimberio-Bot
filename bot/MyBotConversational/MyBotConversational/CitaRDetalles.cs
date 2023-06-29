// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MyBotConversational.ModelsApi;
using System;

namespace MyBotConversational
{
    public class CitaRDetalles
    {
        public long idUsuario { get; set; }

        public string username { get; set; }


        public Veterinario veterinario { get; set; }
        public Mascota mascota { get; set; }

        public string fecha { get; set; }

        public Horario horario { get; set; }

        public string nombreServicio { get; set; }

        public long idEspecialidad { get; set; }
        public long idVeterinario { get; set; }


    }
}
