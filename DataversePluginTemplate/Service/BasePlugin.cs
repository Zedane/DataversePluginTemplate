﻿using DataversePluginTemplate.Service.API;
using DataversePluginTemplate.Service.Extensions;
using Microsoft.Xrm.Sdk;
using System;
using System.Diagnostics;
using System.ServiceModel;

namespace DataversePluginTemplate.Service
{
    /// <summary>
    /// Abstrakte Basisklasse für Plugins, die grundlegende Funktionen und 
    /// Konfigurationsmöglichkeiten bereitstellt.
    /// </summary>
    public abstract class BasePlugin : IPlugin
    {
        // Eine konstante Zeichenfolge, die als Schlüssel für das Zielobjekt in den Eingabeparametern verwendet wird.
        protected const string TARGET = "Target";

        // Unsichere Konfiguration, die von Plugins genutzt werden kann.
        protected string UnsecureConfiguration { get; }

        // Sichere Konfiguration, die von Plugins genutzt werden kann.
        protected string SecureConfiguration { get; }

        /// <summary>
        /// Standardkonstruktor
        /// </summary>
        public BasePlugin() { }


        /// <summary>
        /// Konstruktor, der und die unsichere Konfiguration setzt.
        /// </summary>
        /// <param name="unsecureConfiguration">Die unsichere Konfiguration des Plugins.</param>
        public BasePlugin(string unsecureConfiguration) : this()
        {
            UnsecureConfiguration = unsecureConfiguration;
        }

        /// <summary>
        /// Konstruktor, der die unsichere und die sichere Konfiguration setzt.
        /// </summary>
        /// <param name="unsecureConfiguration">Die unsichere Konfiguration des Plugins.</param>
        /// <param name="secureConfiguration">Die sichere Konfiguration des Plugins.</param>
        public BasePlugin(string unsecureConfiguration, string secureConfiguration)
            : this(unsecureConfiguration)
        {
            SecureConfiguration = secureConfiguration;
        }

        /// <summary>
        /// Führt die Hauptlogik des Plugins basierend auf dem aktuellen Kontext und den übergebenen Parametern aus.
        /// </summary>
        /// <param name="serviceProvider">Der Dienstanbieter, der für den aktuellen Kontext bereitgestellt wird.</param>
        public void Execute(IServiceProvider serviceProvider)
        {
            var pluginContext = new PluginContext(serviceProvider);

            try
            {
                OnExecute(pluginContext);

                switch (pluginContext.ExecutionContext.MessageName)
                {
                    case PluginMessages.CREATE:
                        HandleExecute<Entity>(pluginContext, OnCreate);
                        break;
                    case PluginMessages.UPDATE:
                        HandleExecute<Entity>(pluginContext, OnUpdate);
                        break;
                    case PluginMessages.DELETE:
                        HandleExecute<EntityReference>(pluginContext, OnDelete);
                        break;
                    case PluginMessages.ASSOCIATE:
                        HandleExecute<EntityReference>(pluginContext, OnAssociate);
                        break;
                    case PluginMessages.DISASSOCIATE:
                        HandleExecute<EntityReference>(pluginContext, OnDisassociate);
                        break;
                    default:
                        OnCustomMessage(pluginContext);
                        break;
                }
            }
            catch (APIException apiException)
            {
                DebugLog(pluginContext, $"An {nameof(APIException)} was thrown. {apiException.StackTrace}");
                Log(pluginContext.TracingService, $"{(int)apiException.StatusCode}: {apiException.Message}");
                throw new InvalidPluginExecutionException(apiException.Message, apiException.StatusCode);
            }
            catch (FaultException<OrganizationServiceFault> orgServiceFault)
            {
                DebugLog(pluginContext, $"A {nameof(FaultException)} was thrown. {orgServiceFault.StackTrace}");
                string message = $"[ERROR]: {orgServiceFault}";
                throw new InvalidPluginExecutionException(message, orgServiceFault);
            }
            catch (Exception ex)
            {
                DebugLog(pluginContext, $"A {ex.GetType().Name} was thrown. {ex.StackTrace}");
                string message = $"[ERROR]: {ex.Message}";
                throw new InvalidPluginExecutionException(message, ex);
            }
        }

        protected virtual void OnExecute(PluginContext context) { }

        /// <summary>
        /// Virtuelle Methode, die bei der Erstellung eines Entität ausgeführt wird.
        /// Kann in abgeleiteten Klassen überschrieben werden.
        /// </summary>
        /// <param name="context">Der Plugin-Kontext.</param>
        /// <param name="entity">Die zu erstellende Entität.</param>
        protected virtual void OnCreate(PluginContext context, Entity entity) { }

        /// <summary>
        /// Virtuelle Methode, die bei der Aktualisierung einer Entität ausgeführt wird.
        /// Kann in abgeleiteten Klassen überschrieben werden.
        /// </summary>
        /// <param name="context">Der Plugin-Kontext.</param>
        /// <param name="entity">Die zu aktualisierende Entität.</param>
        protected virtual void OnUpdate(PluginContext context, Entity entity) { }

        /// <summary>
        /// Virtuelle Methode, die bei der Löschung einer Entität ausgeführt wird.
        /// Kann in abgeleiteten Klassen überschrieben werden.
        /// </summary>
        /// <param name="context">Der Plugin-Kontext.</param>
        /// <param name="entityReference">Die Referenz auf die zu löschende Entität.</param>
        protected virtual void OnDelete(PluginContext context, EntityReference entityReference) { }

        /// <summary>
        /// Virtuelle Methode, die bei der Zuordnung einer Entität ausgeführt wird.
        /// Kann in abgeleiteten Klassen überschrieben werden.
        /// </summary>
        /// <param name="context">Der Plugin-Kontext.</param>
        /// <param name="entityReference">Die Referenz auf die zuzuordnende Entität.</param>
        protected virtual void OnAssociate(PluginContext context, EntityReference entityReference) { }

        /// <summary>
        /// Virtuelle Methode, die bei der Aufhebung der Zuordnung einer Entität ausgeführt wird.
        /// Kann in abgeleiteten Klassen überschrieben werden.
        /// </summary>
        /// <param name="context">Der Plugin-Kontext.</param>
        /// <param name="entityReference">Die Referenz auf die zu entfernende Zuordnung.</param>
        protected virtual void OnDisassociate(PluginContext context, EntityReference entityReference) { }

        protected virtual void OnCustomMessage(PluginContext context) { }

        #region Logging

        protected void Log(ITracingService tracingService, string message)
        {
            tracingService.Trace(message);
        }

        protected void Log(ITracingService tracingService, Exception ex)
        {
            Log(tracingService, ex.Message, ex.StackTrace);
        }

        protected void Log(ITracingService tracingService, string message, params string[] args)
        {
            tracingService.Trace(message + "{0}", args);
        }

        protected void Log(PluginContext context, string message)
        {
            Log(context.TracingService, message);
        }

        protected void Log(PluginContext context, Exception ex)
        {
            Log(context.TracingService, ex);
        }

        protected void Log(PluginContext context, string message, params string[] args)
        {
            Log(context.TracingService, message, args);
        }

        protected void DebugLog(PluginContext context, string message)
        {
            context.TracingService.DebugLog(message);
        }

        protected void DebugLogEntity(PluginContext context, Entity entity)
        {
#if DEBUG
            context.TracingService.DebugLogEntity(entity);
#endif
        }

        protected void LogTrace(PluginContext context)
        {
#if DEBUG
            var stackTrace = new StackTrace(1);
            context.TracingService.Trace($"[TRACE]: {stackTrace}");
#endif
        }

        protected void DebugLogSeparator(PluginContext context, string title = "")
        {
            context.TracingService.DebugLogSeparator(title);
        }

        protected void DebugLogSection(PluginContext context)
        {
            context.TracingService.DebugLogSection();
        }

        #endregion


        /// <summary>
        /// Führt die angegebene Aktion für das Zielobjekt im Plugin-Kontext aus.
        /// </summary>
        /// <typeparam name="T">Der Typ des Zielobjekts.</typeparam>
        /// <param name="pluginContext">Der Plugin-Kontext.</param>
        /// <param name="executeCallback">Die Aktion, die ausgeführt werden soll.</param>
        private void HandleExecute<T>(PluginContext pluginContext, Action<PluginContext, T> executeCallback)
        {
            if (pluginContext.ExecutionContext.InputParameters.Contains(TARGET)
                && pluginContext.ExecutionContext.InputParameters[TARGET] is T target)
            {
                executeCallback(pluginContext, target);
            }
        }
    }

    /// <summary>
    /// Abstrakte generische Basisklasse für Plugins, die zusätzliche Typsicherheit für das Zielobjekt bietet.
    /// </summary>
    /// <typeparam name="T">Der Typ des Zielobjekts, mit dem das Plugin arbeitet.</typeparam>
    public abstract class BasePlugin<T> : BasePlugin, IPlugin
    {
        /// <summary>
        /// Standardkonstruktor
        /// </summary>
        public BasePlugin() : base() { }

        /// <summary>
        /// Konstruktor, der den Plugin-Namen und die unsichere Konfiguration setzt.
        /// </summary>
        /// <param name="unsecureConfiguration">Die unsichere Konfiguration des Plugins.</param>
        public BasePlugin(string unsecureConfiguration) : base(unsecureConfiguration) { }

        /// <summary>
        /// Konstruktor, der den Plugin-Namen, die unsichere und die sichere Konfiguration setzt.
        /// </summary>
        /// <param name="unsecureConfiguration">Die unsichere Konfiguration des Plugins.</param>
        /// <param name="secureConfiguration">Die sichere Konfiguration des Plugins.</param>
        public BasePlugin(string unsecureConfiguration, string secureConfiguration)
            : base(unsecureConfiguration, secureConfiguration) { }

        /// <summary>
        /// Führt die Hauptlogik des Plugins basierend auf dem aktuellen Kontext und den übergebenen Parametern aus.
        /// Diese Methode bietet zusätzliche Typsicherheit für das Zielobjekt.
        /// </summary>
        /// <param name="serviceProvider">Der Dienstanbieter, der für den aktuellen Kontext bereitgestellt wird.</param>
        public new void Execute(IServiceProvider serviceProvider)
        {
            var pluginContext = new PluginContext(serviceProvider);

            try
            {
                HandleExecute<T>(pluginContext, OnExecute);

                switch (pluginContext.ExecutionContext.MessageName)
                {
                    case PluginMessages.CREATE:
                        HandleExecute<Entity>(pluginContext, OnCreate);
                        break;
                    case PluginMessages.UPDATE:
                        HandleExecute<Entity>(pluginContext, OnUpdate);
                        break;
                    case PluginMessages.DELETE:
                        HandleExecute<EntityReference>(pluginContext, OnDelete);
                        break;
                    case PluginMessages.ASSOCIATE:
                        HandleExecute<EntityReference>(pluginContext, OnAssociate);
                        break;
                    case PluginMessages.DISASSOCIATE:
                        HandleExecute<EntityReference>(pluginContext, OnDisassociate);
                        break;
                }
            }
            catch (APIException apiException)
            {
                DebugLog(pluginContext, $"An {nameof(APIException)} was thrown. {apiException.StackTrace}");
                Log(pluginContext.TracingService, $"{(int)apiException.StatusCode}: {apiException.Message}");
                throw new InvalidPluginExecutionException(apiException.Message, apiException.StatusCode);
            }
            catch (FaultException<OrganizationServiceFault> orgServiceFault)
            {
                DebugLog(pluginContext, $"A {nameof(FaultException)} was thrown. {orgServiceFault.StackTrace}");
                string message = $"[ERROR]: {orgServiceFault}";
                throw new InvalidPluginExecutionException(message, orgServiceFault);
            }
            catch (Exception ex)
            {
                DebugLog(pluginContext, $"A {ex.GetType().Name} was thrown. {ex.StackTrace}");
                string message = $"[ERROR]: {ex.Message}";
                throw new InvalidPluginExecutionException(message, ex);
            }
        }

        /// <summary>
        /// Virtuelle Methode, die beim Ausführen des Plugins aufgerufen wird.
        /// Kann in abgeleiteten Klassen überschrieben werden.
        /// </summary>
        /// <param name="context">Der Plugin-Kontext.</param>
        /// <param name="target">Das Zielobjekt, mit dem das Plugin arbeitet.</param>
        protected virtual void OnExecute(PluginContext context, T target) { }

        /// <summary>
        /// Führt die angegebene Aktion für das Zielobjekt im Plugin-Kontext aus.
        /// Diese Methode bietet zusätzliche Typsicherheit für das Zielobjekt.
        /// </summary>
        /// <typeparam name="T">Der Typ des Zielobjekts.</typeparam>
        /// <param name="pluginContext">Der Plugin-Kontext.</param>
        /// <param name="executeCallback">Die Aktion, die ausgeführt werden soll.</param>
        private void HandleExecute<T>(PluginContext pluginContext, Action<PluginContext, T> executeCallback)
        {
            if (pluginContext.ExecutionContext.InputParameters.Contains(TARGET)
                && pluginContext.ExecutionContext.InputParameters[TARGET] is T target)
            {
                executeCallback(pluginContext, target);
            }
        }
    }

}
