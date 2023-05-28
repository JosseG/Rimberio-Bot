// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Bot.Builder;
using MyBotConversational.Clu;
using Newtonsoft.Json;

namespace MyBotConversational.CognitiveModels
{
    /// <summary>
    /// An <see cref="IRecognizerConvert"/> implementation that provides helper methods and properties to interact with
    /// the CLU recognizer results.
    /// </summary>
    public class CitaVeterinaria : IRecognizerConvert
    {
        public enum Intent
        {
            registrarCita,
            cancelarCita,
            verCita,
            Salir,
            mostrarInformacion,
            None
        }

        public string Text { get; set; }

        public string AlteredText { get; set; }

        public Dictionary<Intent, IntentScore> Intents { get; set; }

        public CluEntities Entities { get; set; }

        public IDictionary<string, object> Properties { get; set; }

        public void Convert(dynamic result)
        {
            var jsonResult = JsonConvert.SerializeObject(result, new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore});
            var app = JsonConvert.DeserializeObject<CitaVeterinaria>(jsonResult);

            Debug.WriteLine("Convirtiendoo");

            Text = app.Text;
            AlteredText = app.AlteredText;
            Intents = app.Intents;
            Entities = app.Entities;
            Properties = app.Properties;
        }

        public (Intent intent, double score) GetTopIntent()
        {
            var maxIntent = Intent.None;
            var max = 0.0;
            foreach (var entry in Intents)
            {
                if (entry.Value.Score > max)
                {
                    maxIntent = entry.Key;
                    max = entry.Value.Score.Value;
                }
            }
            Debug.WriteLine("Obteniendo top intent");

            return (maxIntent, max);
        }

        public class CluEntities
        {
            public CluEntity[] Entities;

            public CluEntity[] GetMascotaList() => Entities.Where(e => e.Category == "Mascota").ToArray();

            public CluEntity[] GetFechaCitaList() => Entities.Where(e => e.Category == "fechaCita").ToArray();

            public CluEntity[] GetFechaSinFormatoList() => Entities.Where(e => e.Category == "fechaSinFormato").ToArray();

            public CluEntity[] GetIdCitaList() => Entities.Where(e => e.Category == "idCita").ToArray();

            public CluEntity[] GetUsuarioEmailList() => Entities.Where(e => e.Category == "usuarioEmail").ToArray();

            public string GetMascota() => GetMascotaList().FirstOrDefault()?.Text;

            public string GetFechaCita() => GetFechaCitaList().FirstOrDefault()?.Text;

            public string GetFechaSinFormato() => GetFechaSinFormatoList().FirstOrDefault()?.Text;

            public string GetIdCita() => GetIdCitaList().FirstOrDefault()?.Text;

            public string GetUsuarioEmail() => GetUsuarioEmailList().FirstOrDefault()?.Text;
        }
    }
}
