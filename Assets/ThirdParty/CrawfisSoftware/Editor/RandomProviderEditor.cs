using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrawfisSoftware.Unity3D.Utility
{
    [UnityEditor.CustomEditor(typeof(RandomProviderFromList), true)]
    public class RandomProviderEditor : Editor
    {
        //public VisualTreeAsset _uiDocument;
        VisualElement _root;
        RandomProviderFromList _randomProvider;
        [SerializeField] Toggle _generateCheckBox;
        SerializedProperty _checkBoxProperty;
        Button _button;
        SerializedProperty _currentSeedProperty;
        IntegerField _currentSeedField;
        VisualElement _explicitSeedPanel;
        IntListScriptable _fixedSeedList;
        DropdownField _fixedSeedDropDown;
        ObjectField _fixedSeedListField;

        public override VisualElement CreateInspectorGUI()
        {
            _randomProvider = this.target as RandomProviderFromList;
            _fixedSeedList = _randomProvider._fixedSeedList;
            _root = new VisualElement();
            //_uiDocument.CloneTree(root);
            var titleLabel = new Label("Master Random Generator Control");
            _root.Add(titleLabel);

            _fixedSeedListField = new ObjectField("Fixed Seed List");
            _fixedSeedListField.bindingPath = "_fixedSeedList";
            _root.Add(_fixedSeedListField);

            bool autoGenerate = SetupAutoSeedToggle();

            _currentSeedProperty = serializedObject.FindProperty("_currentSeed");
            _currentSeedField = new IntegerField();
            _currentSeedField.label = "Current Seed (display-only)";
            _currentSeedField.value = _currentSeedProperty.intValue;
            _currentSeedField.isReadOnly = true;
            _currentSeedField.RegisterCallback<ChangeEvent<int>>((evt) =>
            {
                int seed = _currentSeedField.value;
                SetSeed(seed);
            });
            _root.Add(_currentSeedField);

            _explicitSeedPanel = new VisualElement();
            _root.Add(_explicitSeedPanel);
            SetupGenerateSeedPanel(_explicitSeedPanel);
            if (autoGenerate) HideExplicitSeedPanel();
            else ShowExplicitSeedPanel();

            return _root;
        }
        public void SetSeed(int seed)
        {
            _randomProvider.SetSeed(seed);
        }

        public void UpdateSeed(int seed)
        {
            SetSeed(seed);
            //_currentSeedProperty = serializedObject.FindProperty("_currentSeed");
            //_currentSeedField.SetValueWithoutNotify(_currentSeedProperty.intValue);
            _currentSeedField.SetValueWithoutNotify(_randomProvider._currentSeed);
        }

        private void SetupGenerateSeedPanel(VisualElement rootPanel)
        {
            var buttonPanel = new VisualElement();
            buttonPanel.style.flexDirection = FlexDirection.Row;
            _button = new UnityEngine.UIElements.Button();
            _button.clicked += () =>
            {
                int seed = new System.Random().Next();
                UpdateSeed(seed);
            };
            _button.text = "Generate New Seed";
            buttonPanel.Add(_button);
            var addToListButton = new UnityEngine.UIElements.Button();
            addToListButton.clicked += () =>
            {
                //_currentSeedProperty = serializedObject.FindProperty("_currentSeed");
                int seed = _randomProvider._currentSeed;
                _fixedSeedList.List.Add(seed);
                _fixedSeedDropDown.choices = _fixedSeedList.List.Select(seed => seed.ToString()).ToList<string>();
            };
            addToListButton.text = "Add Current Seed to List";
            buttonPanel.Add(addToListButton);
            rootPanel.Add(buttonPanel);

            _fixedSeedDropDown = new DropdownField();
            CreateScriptableListOfRandomSeeds();
            _fixedSeedDropDown.choices = _fixedSeedList.List.Select(seed => seed.ToString()).ToList<string>();
            _fixedSeedDropDown.label = "Saved seeds";
            _fixedSeedDropDown.tooltip = "You can associate a ScriptableObject that contains a list of saved random seed. See IntListScriptable.cs";
            _fixedSeedDropDown.value = "Select a fixed Seed";
            rootPanel.Add(_fixedSeedDropDown);
            _fixedSeedDropDown.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                int seed = int.Parse(_fixedSeedDropDown.value);
                UpdateSeed(seed);
                _randomProvider.SeedIndex = _fixedSeedDropDown.index;
            });
        }

        private void CreateScriptableListOfRandomSeeds()
        {
            if (_fixedSeedList != null) return;
            _fixedSeedList = ScriptableObject.CreateInstance<IntListScriptable>();
            _fixedSeedList.List = new List<int>();
            // Bug: If the "Scriptables" directory does not exist this will throw an error.
            string seedListPath = "Assets/Scriptables/fixedSeedsList.asset";
            AssetDatabase.CreateAsset(_fixedSeedList, seedListPath);
        }

        private bool SetupAutoSeedToggle()
        {
            _checkBoxProperty = serializedObject.FindProperty("_generateNewSeed");
            _generateCheckBox = new UnityEngine.UIElements.Toggle();
            _generateCheckBox.tooltip = "If true, generates a new seed each time this class is run. If false, it uses the fixed seed.";
            _generateCheckBox.label = "Auto-generate new seed on play";
            _generateCheckBox.value = _checkBoxProperty.boolValue;
            _generateCheckBox.RegisterCallback<ChangeEvent<bool>>((toggleAutoSeed) =>
                {
                    if (toggleAutoSeed.newValue) HideExplicitSeedPanel();
                    else ShowExplicitSeedPanel();
                    _randomProvider.SetAutoGenerateSeed(toggleAutoSeed.newValue);
                },
                TrickleDown.NoTrickleDown);

            _root.Add(_generateCheckBox);
            return _generateCheckBox.value;
        }

        private void ShowExplicitSeedPanel()
        {
            _button.SetEnabled(true);
            _currentSeedField.SetEnabled(true);
            _currentSeedField.label = "Current Seed ";
            _currentSeedField.isReadOnly = false;
            _explicitSeedPanel.visible = true;
        }

        private void HideExplicitSeedPanel()
        {
            _button.SetEnabled(false);
            _currentSeedField.SetEnabled(false);
            _currentSeedField.label = "Current Seed (read-only)";
            _currentSeedField.isReadOnly = true;
            _explicitSeedPanel.visible = false;
        }
    }
}