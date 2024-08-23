using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using GraphQL.Utilities.Federation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace phx_shipment_demo
{
    public partial class MainWindow : Window
    {
        /**
         * Readme!!!
         * Useful Plugin for VS:
         * https://marketplace.visualstudio.com/items?itemName=codearchitects-research.GraphQLTools
         * 
         * Readme about GraphQL:
         * https://github.com/graphql-dotnet/graphql-client
         **/

        public MainWindow()
        {
            InitializeComponent();

            tbUrl.Text = Properties.Settings.Default.url;
            tbUser.Text = Properties.Settings.Default.user;
            tbPass.Text = Properties.Settings.Default.pass;

            if (string.IsNullOrEmpty(tbUrl.Text))
            {
                tbUrl.Text = "https://subdomain.phx-erp.de/backend-api/admin-api";
            }
        }

        private GraphQLHttpClient graphQLClient;
        public GraphQLHttpClient GraphQLClient
        {
            get
            {
                if (graphQLClient == null)
                {
                    graphQLClient = initClient();
                }
                return graphQLClient;
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.url = tbUrl.Text;
            Properties.Settings.Default.pass = tbPass.Text;
            Properties.Settings.Default.user = tbUser.Text;
            Properties.Settings.Default.Save();
        }

        public GraphQLHttpClient initClient()
        {
            //SECURITY WARNING: This is a workaround to ignore SSL certificate errors on local systems. Do not use in production!
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            var client = new HttpClient(clientHandler);
            return new GraphQLHttpClient(tbUrl.Text, new NewtonsoftJsonSerializer(), client);
        }

        public async Task Login()
        {
            var req = new GraphQLRequest
            {
                Query = """
                mutation Login($username: String!, $password: String!) {
                    login(username: $username, password: $password) {
                        ... on CurrentUser {
                              id
                              identifier
                        }
                    }
                }
                """,

                OperationName = "Login",
                Variables = new
                {
                    username = tbUser.Text,
                    password = tbPass.Text
                }
            };

            try
            {
                var result = await this.GraphQLClient.SendMutationAsync<JObject>(req);

                displayResult(result);

                var responseHeaders = result.AsGraphQLHttpResponse().ResponseHeaders;

                var authHeader = responseHeaders.FirstOrDefault(x => x.Key == "Authorization");

                if (authHeader.Value != null)
                {
                    this.GraphQLClient.HttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + authHeader.Value.ToString());
                }
            }
            catch (Exception ex)
            {
                this.tbOutput.Text = ex.Message;
            }
        }

        void displayResult(GraphQLResponse<JObject> result)
        {
            if (result.Errors?.Length > 0)
            {
                this.tbOutput.Text = JsonConvert.SerializeObject(result.Errors[0]);
            }

            this.tbOutput.Text = result.Data.ToString();
        }

        #region Artikel

        private async void btnArtikelHolen_Click(object sender, RoutedEventArgs e)
        {
            var getProductsRequest = new GraphQLRequest
            {
                Query = """
                query GetProducts($input: ProductSearchInput) {
                    getProducts(input: $input) {
                        totalItems
                        items {
                            id
                            identifier
                            description
                        }
                    }
                }
                """,

                OperationName = "GetProducts",
                Variables = new
                {
                    input = new
                    {
                        take = 20,
                        identifier = tbArtikelnummer.Text?.Length > 0 ? tbArtikelnummer.Text : null
                    }
                }
            };

            var result = await this.GraphQLClient.SendQueryAsync<JObject>(getProductsRequest);

            displayResult(result);
        }

        #endregion

        #region Kunden

        private async void btnKundenHolen_Click(object sender, RoutedEventArgs e)
        {
            var getProductsRequest = new GraphQLRequest
            {
                Query = """
                query GetAddresses($input: AddressSearchInput) {
                    getAddresses(input: $input) {
                        totalItems
                        items {
                            id
                            name
                            lastName
                            firstName
                            debitor {
                                identifier
                            }
                            defaultEmail
                            mainPhone
                            primaryPostalAddress {
                                recipient
                                addition
                                street
                                postalCode
                                city
                                email
                                country {
                                    identifier
                                }
                            }
                        }
                    }
                }
                """,

                OperationName = "GetAddresses",
                Variables = new
                {
                    input = new
                    {
                        take = 20,
                        debitorIdentifier = tbKundennummer.Text?.Length > 0 ? tbKundennummer.Text : null,
                    }
                }
            };

            var result = await this.GraphQLClient.SendQueryAsync<JObject>(getProductsRequest);

            displayResult(result);
        }

        #endregion

        #region Lieferschein und Pakete
        private async void btnLieferscheineHolen_Click(object sender, RoutedEventArgs e)
        {
            var getProductsRequest = new GraphQLRequest
            {
                Query = """
                query GetDocuments($input: DocumentSearchInput) {
                    getDocuments(input: $input) {
                        totalItems
                        items {
                            id
                            identifier
                            date                                      
                            deliveryDate
                            estimatedDeliveryDate
                            address {
                                id
                                name
                                debitor {
                                    identifier
                                }
                            }
                            deliveryDate
                            estimatedDeliveryDate
                            deliveryTerms {
                              id
                              identifier
                            }
                            shippingMethod {
                              id
                              identifier
                              description
                            }
                            deliveryNotes
                        }
                    }
                }
                """,

                OperationName = "GetDocuments",
                Variables = new
                {
                    input = new
                    {
                        take = 20,
                        documentDefinitionIdentifier = "DN",
                        identifier = tbLieferscheinNummer.Text
                        // Filter für Status
                        // documentStatus = "[\"IW\"]"
                    }
                }
            };

            var result = await this.GraphQLClient.SendQueryAsync<JObject>(getProductsRequest);

            displayResult(result);
        }

        private async void btnPaketeHolen_Click(object sender, RoutedEventArgs e)
        {
            var getDeliveryItemsRequest = new GraphQLRequest
            {
                Query = """
                query GetDeliveryItems($input: DeliveryItemSearchInput) {
                    getDeliveryItems(input: $input) {
                        totalItems
                        items {
                            id
                            trackingNumber
                        }
                    }
                }
                """,

                OperationName = "GetDeliveryItems",
                Variables = new
                {
                    input = new
                    {
                        documentId = tbLieferscheinIdFuerTrackingNummer.Text,
                    }
                }
            };

            var result = await this.GraphQLClient.SendQueryAsync<JObject>(getDeliveryItemsRequest);

            displayResult(result);
        }

        private async void btnPaketZuLieferschein_Click(object sender, RoutedEventArgs e)
        {
            var saveDeliveryItem = new GraphQLRequest
            {
                Query = """
                mutation SaveDeliveryItemToDocument($input: DeliveryItemToDocumentInput!) {
                    saveDeliveryItemToDocument(input: $input) {
                        deliveryItem {
                            id
                            trackingNumber
                        }
                    }
                }
                """,

                OperationName = "SaveDeliveryItemToDocument",
                Variables = new
                {
                    input = new
                    {
                        deliveryItem = new
                        {
                            trackingNumber = tbTrackingNummer.Text,
                        },
                        documentId = tbLieferscheinIdFuerTrackingNummer.Text,
                    }
                }
            };

            var result = await this.GraphQLClient.SendMutationAsync<JObject>(saveDeliveryItem);

            displayResult(result);
        }

        private async void btnPaketLoeschen_Click(object sender, RoutedEventArgs e)
        {
            var deleteDeliveryItem = new GraphQLRequest
            {
                Query = """
                mutation DeleteDeliveryItem($id: String!) {
                    deleteDeliveryItem(id: $id) {
                        result
                        message
                    }
                }
                """,
                OperationName = "DeleteDeliveryItem",
                Variables = new
                {
                    id = tbPaketId.Text
                }
            };

            var result = await this.GraphQLClient.SendMutationAsync<JObject>(deleteDeliveryItem);

            displayResult(result);
        }

        #endregion

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(tbUser.Text) || string.IsNullOrEmpty(tbPass.Text))
            {
                MessageBox.Show("Bitte Benutzername und Passwort eingeben");
                return;
            }

            if (string.IsNullOrEmpty(tbUrl.Text))
            {
                MessageBox.Show("Bitte URL eingeben");
                return;
            }

            await this.Login();
        }
    }
}