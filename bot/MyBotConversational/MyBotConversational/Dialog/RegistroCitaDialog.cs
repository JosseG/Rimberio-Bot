using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Json;
using MyBotConversational.ModelsApi;
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Collections.Generic;

namespace MyBotConversational.Dialog
{
    public class RegistroCitaDialog : CancelAndHelpDialog
    {

        private readonly HttpClient _httpClient;

        private const string IntroStepMsgText = "Escogio la intención de registrar cita";
        private const string CodigoUsuarioStepMsgText = "¿Cual es su código de usuario?";
        private const string MascotaStepMsgText = "¿Que mascota va a traer?";

        public RegistroCitaDialog(HttpClient httpClient)
            : base(nameof(RegistroCitaDialog))
        {

            this._httpClient = httpClient;
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                IdUsuarioStepAsync,
                VerifyIdUsuarioStepAsync,
                AnimalStepAsync,
                ServiciosInfoStepAsync,
                FechaStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }



        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var message = MessageFactory.Text(IntroStepMsgText, IntroStepMsgText, InputHints.ExpectingInput);
            await stepContext.Context.SendActivityAsync(message, cancellationToken);

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> IdUsuarioStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;





            /* _httpClient.BaseAddress = new Uri($@"http://127.0.0.1:8080/api/v1/reservaciones/usuario/{citaRDetalles.idUsuario}");
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));


            using HttpResponseMessage response = await _httpClient.GetAsync(_httpClient.BaseAddress);

            var content = await ValidateContent(response).ReadAsStringAsync();

            Usuario usuario = JsonSerializer.Deserialize<Usuario>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            /*Usuario usuario = await _httpClient.GetFromJsonAsync<Usuario>(_httpClient.BaseAddress);*/

            /*if (usuario != null)
            {
                citaRDetalles.idUsuario = usuario.codigo;
                Debug.WriteLine("1---------------------------------------");
                Debug.WriteLine(usuario.codigo);
            }
            */


            if (citaRDetalles.idUsuario == 0 || citaRDetalles.idUsuario == -1)
            {

                Debug.WriteLine("2---------------------------------------");
                var promptMessage = MessageFactory.Text(CodigoUsuarioStepMsgText, CodigoUsuarioStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(citaRDetalles.idUsuario, cancellationToken);
        }

        private async Task<DialogTurnResult> VerifyIdUsuarioStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;
            citaRDetalles.idUsuario = long.Parse((string)stepContext.Result);

            /*_httpClient.BaseAddress = new Uri($@"http://127.0.0.1:8080/api/v1/reservaciones/usuario/{citaRDetalles.idUsuario}");
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));*/

            Debug.WriteLine("ADDRESS S" + _httpClient.BaseAddress);



            /*using HttpResponseMessage response = await _httpClient.GetAsync(_httpClient.BaseAddress);*/



            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"http://127.0.0.1:8080/api/v1/reservaciones/usuario/{citaRDetalles.idUsuario}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            Usuario usuario = JsonSerializer.Deserialize<Usuario>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            /*Usuario usuario = await _httpClient.GetFromJsonAsync<Usuario>(_httpClient.BaseAddress);*/

            if (usuario == null)
            {
                /*citaRDetalles.idUsuario = usuario.codigo; Debug.WriteLine(usuario.codigo);*/
                Debug.WriteLine("1---------------------------------------");
                stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 2;

            }
            else
            {
                citaRDetalles.idUsuario = usuario.codigo;
            }




            return await stepContext.NextAsync(citaRDetalles.idUsuario, cancellationToken);
        }

        private async Task<DialogTurnResult> AnimalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;


            Debug.WriteLine("El RESULTADO DEL PASO ANTERIOR ES -> " + stepContext.Result);

            citaRDetalles.idUsuario = (long)stepContext.Result;


            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"http://127.0.0.1:8080/api/v1/reservaciones/mascota/usuario/{citaRDetalles.idUsuario}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            List<Mascota> mascotas = JsonSerializer.Deserialize<List<Mascota>>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });



            string listastringmascotas = "";

            for( int i = 0;i< mascotas.Count;i++)
            {
                listastringmascotas = listastringmascotas + " " + mascotas[i].nombre;
            }


            var Texto = $"Tienes estas mascotas registradas, dígite el nombre de una de ellas como se visualiza aquí -> {listastringmascotas}";

            if (string.IsNullOrEmpty(citaRDetalles.nombreMascota))
            {
                var promptMessage = MessageFactory.Text(Texto, Texto, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(citaRDetalles.nombreMascota, cancellationToken);
        }




        private async Task<DialogTurnResult> ServiciosInfoStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;

            citaRDetalles.nombreMascota = (string)stepContext.Result;



            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("http://127.0.0.1:8080/api/v1/reservaciones/servicios"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            List<Servicio> servicios = JsonSerializer.Deserialize<List<Servicio>>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });



            string listastringservicios = "";

            for (int i = 0; i < servicios.Count; i++)
            {
                listastringservicios += servicios[i].tipo + " con código -> [" + servicios[i].codigo + "], ";
            }


            var Texto = $"Tenemos disponible los siguientes servicios, dígite exactamente el nombre del tipo de servicio de uno de ellos -> {listastringservicios}";

            if (string.IsNullOrEmpty(citaRDetalles.nombreServicio))
            {
                var promptMessage = MessageFactory.Text(Texto, Texto, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(citaRDetalles.nombreServicio, cancellationToken);
        }

        private async Task<DialogTurnResult> FechaStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;

            citaRDetalles.nombreServicio = (string)stepContext.Result;

            Debug.WriteLine("Si esta entrando aqui");


            HttpRequestMessage msg = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($@"http://127.0.0.1:8080/api/v1/reservaciones/horario/service/{citaRDetalles.nombreServicio}"),
            };

            var response = await _httpClient.SendAsync(msg);

            var content = await ValidateContent(response).ReadAsStringAsync();

            List<Horario> horarios = JsonSerializer.Deserialize<List<Horario>>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });



            string listastringhorarios = "";

            for (int i = 0; i < horarios.Count; i++)
            {
                listastringhorarios += horarios[i].fecha + " con código -> [" + horarios[i].codigo + "], ";
            }
            var Texto = $"Tenemos disponible los siguientes horarios para ese servicio, dígite exactamente el horario de uno de ellos -> {listastringhorarios}";

            if (citaRDetalles.idFecha == 0 || citaRDetalles.idFecha == -1)
            {
                var promptMessage = MessageFactory.Text(Texto, Texto, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(citaRDetalles.idFecha, cancellationToken);
        }

        /*private async Task<DialogTurnResult> FechaConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;

            citaRDetalles.fecha = (string)stepContext.Result;

            Debug.WriteLine(citaRDetalles.fecha);

            if (citaRDetalles.fecha == null || IsAmbiguous(citaRDetalles.fecha))
            {
                return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), citaRDetalles.fecha, cancellationToken);
            }

            return await stepContext.NextAsync(citaRDetalles.fecha, cancellationToken);
        }¨*/


        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var citaRDetalles = (CitaRDetalles)stepContext.Options;

            citaRDetalles.idFecha = long.Parse((string)stepContext.Result);

            var messageText = $"Por favor confirma, el id del usuario es ->  {citaRDetalles.idUsuario} , la mascota es -> {citaRDetalles.nombreMascota} y el código de la fecha es -> {citaRDetalles.idFecha}. ¿Es correcto?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var citaRDetalles = (CitaRDetalles)stepContext.Options;

                var parameters = new
                {
                    idUsuario = citaRDetalles.idUsuario,
                    nombreMascota = citaRDetalles.nombreMascota,
                    idFecha = citaRDetalles.idFecha
                };

                HttpRequestMessage msg = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("http://127.0.0.1:8080/api/v1/reservaciones"),
                    Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(parameters), Encoding.UTF8, "application/json")

                };

                var response = await _httpClient.SendAsync(msg);

                return await stepContext.EndDialogAsync(citaRDetalles, cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private static bool IsAmbiguous(string timex)
        {
            var timexProperty = new TimexProperty(timex);
            return !timexProperty.Types.Contains(Constants.TimexTypes.DateTime);
        }


        private HttpContent ValidateContent(HttpResponseMessage response)
        {
            if (string.IsNullOrEmpty(response.Content?.ReadAsStringAsync().Result))
            {
                return response.Content = new StringContent("null", Encoding.UTF8, MediaTypeNames.Application.Json);
            }
            else
            {
                return response.Content;
            }
        }

    }
}
