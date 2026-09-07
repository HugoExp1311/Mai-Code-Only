using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Base.Localization.Data;
using TMPro;
using UnityEngine;

namespace Base.Localization
{
    /// <summary>
    /// TextMeshPro localization component backed by runtime CSV files.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string componentId;

        [Header("Localization Settings")]
        [SerializeField] private bool excludeFromLocalization = false;
        [SerializeField] private string tableName;
        [SerializeField] private string key;

        private TMP_Text textComponent;
        private Dictionary<string, object> variables;
        private bool isDirect;
        private string directContent;
        private bool hasPendingUpdate;
        private bool isRegistered;
        private LocalizationMode currentMode = LocalizationMode.Static;
        private object currentData;
        private string currentTemplateKey;
        private string currentSeparator = "\n";
        private Func<object, Dictionary<string, object>> currentItemConverter;

        public string ComponentId => componentId;
        public string TableName => tableName;
        public string Key => key;
        public bool HasPendingUpdate => hasPendingUpdate;
        public bool ExcludeFromLocalization => excludeFromLocalization;

#if UNITY_EDITOR
        public string EditorPreviewDomain => string.IsNullOrEmpty(tableName)
            ? LocalizationDomains.UI
            : LocalizationDomains.Normalize(tableName);

        public bool EditorCanPreview => !excludeFromLocalization && !string.IsNullOrEmpty(key);

        public TMP_Text EditorTextComponent
        {
            get
            {
                EnsureTextComponent();
                return textComponent;
            }
        }
#endif

        private void Awake()
        {
            textComponent = GetComponent<TMP_Text>();
            if (textComponent == null)
            {
                Debug.LogError($"[LocalizedText] No TMP_Text component found on {gameObject.name}");
                return;
            }

            if (string.IsNullOrEmpty(componentId))
                componentId = GenerateComponentId();
        }

        private void OnEnable()
        {
            Register();

            if (hasPendingUpdate)
                ApplyPending();
            else if (!excludeFromLocalization && !string.IsNullOrEmpty(key))
                ApplyLocalization();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        public void SetLocalized(string key, string tableName = null, Dictionary<string, object> variables = null)
        {
            if (excludeFromLocalization)
                return;

            this.key = key;
            this.tableName = LocalizationDomains.Normalize(tableName);
            this.variables = variables;
            isDirect = false;
            directContent = null;
            currentMode = variables != null && variables.Count > 0 ? LocalizationMode.SmartString : LocalizationMode.Static;
            currentData = variables;
            currentTemplateKey = null;
            currentItemConverter = null;

            ApplyOrMarkPending();
        }

        public void SetDirect(string content)
        {
            isDirect = true;
            directContent = content ?? string.Empty;
            currentMode = LocalizationMode.Direct;
            currentData = null;
            variables = null;

            if (isActiveAndEnabled)
            {
                EnsureTextComponent();
                if (textComponent != null)
                    textComponent.text = directContent;
                hasPendingUpdate = false;
            }
            else
            {
                hasPendingUpdate = true;
            }
        }

        public void SetLocalizedList<T>(
            string templateKey,
            IEnumerable<T> items,
            Func<T, Dictionary<string, object>> itemToVariables,
            string tableName = null,
            string separator = "\n")
        {
            if (string.IsNullOrEmpty(templateKey))
            {
                Debug.LogError("[LocalizedText] SetLocalizedList: templateKey is null or empty");
                return;
            }

            if (items == null)
            {
                Debug.LogWarning("[LocalizedText] SetLocalizedList: items is null");
                SetDirect(string.Empty);
                return;
            }

            if (itemToVariables == null)
            {
                Debug.LogError("[LocalizedText] SetLocalizedList: itemToVariables is null");
                return;
            }

            key = templateKey;
            this.tableName = LocalizationDomains.Normalize(tableName);
            currentTemplateKey = templateKey;
            currentSeparator = separator ?? "\n";
            currentMode = LocalizationMode.DynamicList;
            currentData = items.Cast<object>().ToList();
            currentItemConverter = item => itemToVariables((T)item);
            isDirect = false;
            directContent = null;
            variables = null;

            ApplyOrMarkPending();
        }

        public void SetLocalizedTemplate(
            string templateKey,
            IEnumerable<Dictionary<string, object>> variableSets,
            string tableName = null,
            string separator = "\n")
        {
            if (string.IsNullOrEmpty(templateKey))
            {
                Debug.LogError("[LocalizedText] SetLocalizedTemplate: templateKey is null or empty");
                return;
            }

            if (variableSets == null)
            {
                Debug.LogWarning("[LocalizedText] SetLocalizedTemplate: variableSets is null");
                SetDirect(string.Empty);
                return;
            }

            key = templateKey;
            this.tableName = LocalizationDomains.Normalize(tableName);
            currentTemplateKey = templateKey;
            currentSeparator = separator ?? "\n";
            currentMode = LocalizationMode.DynamicList;
            currentData = variableSets.ToList();
            currentItemConverter = null;
            isDirect = false;
            directContent = null;
            variables = null;

            ApplyOrMarkPending();
        }

        public void Refresh()
        {
            if (excludeFromLocalization)
                return;

            if (!isActiveAndEnabled)
            {
                hasPendingUpdate = true;
                return;
            }

            ApplyLocalization();
        }

        public void ApplyPending()
        {
            if (!hasPendingUpdate)
                return;

            hasPendingUpdate = false;
            ApplyLocalization();
        }

        public LocalizationSettings GetSettings()
        {
            return new LocalizationSettings
            {
                Key = key,
                TableName = tableName,
                Variables = variables,
                IsDirect = isDirect,
                DirectContent = directContent,
                Mode = currentMode,
                TemplateKey = currentTemplateKey,
                Separator = currentSeparator,
                ListData = currentData,
                EnableCaching = true,
                CacheDuration = 0f
            };
        }

        private void ApplyOrMarkPending()
        {
            if (isActiveAndEnabled)
            {
                ApplyLocalization();
                hasPendingUpdate = false;
            }
            else
            {
                hasPendingUpdate = true;
            }
        }

        private void ApplyLocalization()
        {
            EnsureTextComponent();
            if (textComponent == null)
                return;

            switch (currentMode)
            {
                case LocalizationMode.Direct:
                    textComponent.text = directContent ?? string.Empty;
                    break;
                case LocalizationMode.DynamicList:
                    textComponent.text = RenderDynamicList();
                    break;
                case LocalizationMode.SmartString:
                case LocalizationMode.Static:
                default:
                    ApplyStaticOrSmart();
                    break;
            }
        }

        private void ApplyStaticOrSmart()
        {
            if (string.IsNullOrEmpty(key))
            {
                textComponent.text = string.Empty;
                return;
            }

            string domain = ResolveDomain();
            textComponent.text = LocalizationManager.Instance.GetLocalizedString(key, domain, variables);
        }

        private string RenderDynamicList()
        {
            if (string.IsNullOrEmpty(currentTemplateKey))
                return string.Empty;

            string domain = ResolveDomain();

            if (currentData is IEnumerable<Dictionary<string, object>> variableSets)
                return LocalizationManager.Instance.Templates.RenderTemplate(currentTemplateKey, variableSets, domain, currentSeparator);

            if (currentData is IEnumerable list && currentItemConverter != null)
            {
                var rendered = new List<string>();
                foreach (object item in list)
                {
                    Dictionary<string, object> itemVariables = currentItemConverter(item);
                    rendered.Add(LocalizationManager.Instance.GetLocalizedString(currentTemplateKey, domain, itemVariables));
                }

                return string.Join(currentSeparator, rendered);
            }

            return string.Empty;
        }

        private string ResolveDomain()
        {
            if (!string.IsNullOrEmpty(tableName))
                return LocalizationDomains.Normalize(tableName);

            return LocalizationManager.Instance?.Config != null
                ? LocalizationDomains.Normalize(LocalizationManager.Instance.Config.defaultUITable)
                : LocalizationDomains.UI;
        }

        private void Register()
        {
            if (LocalizationManager.Instance != null && !isRegistered)
            {
                LocalizationManager.Instance.RegisterComponent(this);
                isRegistered = true;
            }
        }

        private void Unregister()
        {
            if (LocalizationManager.Instance != null && isRegistered)
            {
                LocalizationManager.Instance.UnregisterComponent(this);
                isRegistered = false;
            }
        }

        private void EnsureTextComponent()
        {
            if (textComponent == null)
                textComponent = GetComponent<TMP_Text>();
        }

        private string GenerateComponentId()
        {
            string path = gameObject.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(tableName))
                tableName = LocalizationDomains.Normalize(tableName);
        }
    }
}
