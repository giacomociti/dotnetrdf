﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;
using VDS.RDF.Query;
using VDS.RDF.Query.Inference.Pellet;
using VDS.RDF.Query.Inference.Pellet.Services;
using VDS.RDF.Update;
using VDS.RDF.Writing.Formatting;

namespace wp7_tests
{
    public partial class MainPage : PhoneApplicationPage
    {
        private SparqlRemoteEndpoint _endpoint;
        private SparqlRemoteUpdateEndpoint _updateEndpoint;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            //Set up our test endpoints
            this._endpoint = new SparqlRemoteEndpoint(new Uri("http://dbpedia.org/sparql"), "http://dbpedia.org");
            this._updateEndpoint = new SparqlRemoteUpdateEndpoint(new Uri("http://localhost/demos/server/update"));
        }

        #region Callbacks

        private void GraphCallback(IGraph g, Object state)
        {
            Dispatcher.BeginInvoke(() =>
                {
                    TurtleFormatter formatter = new TurtleFormatter(g.NamespaceMap);
                    this.ResultsSummary.Text = g.Triples.Count + " Triple(s) returned";
                    this.ResultsList.Items.Clear();
                    foreach (Triple t in g.Triples)
                    {
                        this.ResultsList.Items.Add(t.ToString(formatter));
                    }
                });
        }

        private void SparqlResultsCallback(SparqlResultSet results, Object state)
        {
            Dispatcher.BeginInvoke(() =>
                {
                    DateTime? start = state as DateTime?;
                    if (start != null)
                    {
                        TimeSpan elapsed = DateTime.Now - start.Value;
                        //Do what you want with the execution time...
                    }

                    SparqlFormatter formatter = new SparqlFormatter();
                    this.ResultsSummary.Text = results.Count + " Result(s) returned";
                    this.ResultsList.Items.Clear();

                    switch (results.ResultsType)
                    {
                        case SparqlResultsType.Boolean:
                            this.ResultsList.Items.Add(formatter.FormatBooleanResult(results.Result));
                            break;
                        case SparqlResultsType.VariableBindings:
                            foreach (SparqlResult r in results)
                            {
                                this.ResultsList.Items.Add(r.ToString(formatter));
                            }
                            break;
                        default:
                            this.ResultsList.Items.Add("Unknown Results Type");
                            break;
                    }
                });
            
        }

        private void RdfHandlerCallback(IRdfHandler handler, Object state)
        {
            Dispatcher.BeginInvoke(() =>
                {
                    this.ResultsList.Items.Clear();

                    if (handler is CountHandler)
                    {
                        this.ResultsSummary.Text = "Handler counted " + ((CountHandler)handler).Count + " Triple(s)";
                    }
                    else
                    {
                        this.ResultsSummary.Text = "Parsing with " + handler.GetType().Name + " Completed";
                    }
                });
        }

        private void TripleStoreCallback(ITripleStore store, Object state)
        {
            Dispatcher.BeginInvoke(() =>
                {
                    TurtleFormatter formatter = new TurtleFormatter(new NamespaceMapper());
                    this.ResultsSummary.Text = store.Graphs.Count + " Graph(s) with " + store.Graphs.Sum(g => g.Triples.Count) + " Triple(s)";
                    this.ResultsList.Items.Clear();
                    foreach (IGraph g in store.Graphs)
                    {
                        this.ResultsList.Items.Add("Graph " + formatter.FormatUri(g.BaseUri));
                        foreach (Triple t in g.Triples)
                        {
                            this.ResultsList.Items.Add(t.ToString(formatter));
                        }
                    }
                });
        }

        private void UpdateCallback(Object state)
        {
            Dispatcher.BeginInvoke(() =>
                {
                    this.ResultsList.Items.Clear();
                    this.ResultsSummary.Text = "Update Completed OK";
                });
        }

        private void PelletServerReadyCallback(PelletServer server, Object state)
        {
            Dispatcher.BeginInvoke(() =>
                {
                    this.ResultsSummary.Text = server.KnowledgeBases.Count() + " Knowledge Bases available";
                    this.ResultsList.Items.Clear();
                    foreach (KnowledgeBase kb in server.KnowledgeBases)
                    {
                        this.ResultsList.Items.Add(kb.Name);
                        foreach (PelletService svc in kb.Services)
                        {
                            this.ResultsList.Items.Add(kb.Name + "/" + svc.Name);
                        }
                    }
                });
        }

        #endregion

        #region Test Methods

        private void RemoteSparqlTest1_Click(object sender, RoutedEventArgs e)
        {
            this._endpoint.QueryWithResultSet("SELECT ?type WHERE { ?s a ?type } LIMIT 10", this.SparqlResultsCallback, null);
        }

        private void RemoteSparqlTest2_Click(object sender, RoutedEventArgs e)
        {
            this._endpoint.QueryWithResultGraph("CONSTRUCT { ?s a ?type } WHERE { ?s a ?type } LIMIT 10", this.GraphCallback, null);
        }

        private void UriLoaderTest1_Click(object sender, RoutedEventArgs e)
        {
            UriLoader.Load(new Graph(), new Uri("http://dbpedia.org/resource/Ilkeston"), this.GraphCallback, null);
        }

        private void UriLoaderTest2_Click(object sender, RoutedEventArgs e)
        {
            CountHandler handler = new CountHandler();
            UriLoader.Load(handler, new Uri("http://dbpedia.org/resource/Ilkeston"), this.RdfHandlerCallback, null);
        }

        private void UriLoaderTest3_Click(object sender, RoutedEventArgs e)
        {
            UriLoader.Load(new TripleStore(), new Uri("http://localhost/demos/sampleDataset"), this.TripleStoreCallback, null);
        }

        private void UriLoaderTest4_Click(object sender, RoutedEventArgs e)
        {
            CountHandler handler = new CountHandler();
            UriLoader.LoadDataset(handler, new Uri("http://localhost/demos/sampleDataset"), this.RdfHandlerCallback, null);
        }

        private void RemoteSparqlUpdateTest1_Click(object sender, RoutedEventArgs e)
        {
            this._updateEndpoint.Update("LOAD <http://dbpedia.org/resource/Ilkeston> INTO GRAPH <http://example.org/ilson>", this.UpdateCallback, null);
        }

        #endregion

        private void PelletServerTest_Click(object sender, RoutedEventArgs e)
        {
            PelletServer server = new PelletServer("http://ps.clarkparsia.com", this.PelletServerReadyCallback, null);
        }

        private void PelletClassifyTest_Click(object sender, RoutedEventArgs e)
        {
            PelletServer server = new PelletServer("http://ps.clarkparsia.com", (svr,_) =>
                {
                    Type target = typeof(ClassifyService);
                    ClassifyService svc = svr.KnowledgeBases.First(kb => kb.SupportsService(target)).GetService<ClassifyService>();
                    svc.Classify(this.GraphCallback, null);
                }, null);
        }
    }
}