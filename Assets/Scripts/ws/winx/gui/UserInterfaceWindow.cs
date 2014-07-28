using System;
using UnityEngine;
using ws.winx.input;
using System.Collections.Generic;
using System.IO;
using System.Collections;

namespace ws.winx.gui
{
    
    public class UserInterfaceWindow:MonoBehaviour
    {
		protected Rect _buttonRect = new Rect (0, 0, 100, 15);
		protected Rect _layerLabelRect = new Rect (0, 0, 100, 15);
        protected Dictionary<int, InputState> _stateInputCombinations;

        public Dictionary<int, InputState> StateInputCombinations
        {
            get { return _stateInputCombinations; }
            set { _stateInputCombinations = value; }
        }
		protected static bool _settingsLoaded=false;
		protected int _selectedStateHash = 0;
		protected string _combinationSeparator=InputAction.SPACE_DESIGNATOR.ToString();
		protected int _isPrimary = 0;
		protected string _currentInputString;
		protected GUILayoutOption[] _inputLabelStyle = new GUILayoutOption[]{ GUILayout.Width (200)};
		protected GUILayoutOption[] _stateNameLabelStyle = new GUILayoutOption[]{ GUILayout.Width (250)};
		protected InputAction _action;
		protected Vector2 _scrollPosition=Vector2.zero;
		protected InputCombination _previousStateInput = null;

		
		
		
		

		public int maxCombosNum = 3;
		public GUISkin guiSkin;
		public TextAsset settingsXML;
		//public bool allowDuplicates=false;

        void Start()
        {
            
        
            if (!_settingsLoaded && settingsXML != null)
            {
                loadInputSettings();
                _settingsLoaded = true;
            }


        
        }

		
		/// <summary>
		/// Update this instance.
		/// </summary>
		void Update(){

            if (_selectedStateHash != 0)
            {
               // UnityEngine.Debug.Log("Edit mode true");
                //Use is mapping states so no quering keys during gameplay
                InputManager.EditMode = true;

                _action = InputEx.GetInput();

                if (_action != null && (_action.code ^ (int)KeyCode.Escape) != 0 && (_action.code ^ (int)KeyCode.Return) != 0)
                {


                    if ((_action.code ^ (int)KeyCode.Backspace) == 0)
                    {
                        _stateInputCombinations[_selectedStateHash].combinations[_isPrimary].Clear();
                        _stateInputCombinations[_selectedStateHash].combinations[_isPrimary].Add(new InputAction(KeyCode.None));
                    }
                    else
                    {
                        toInputCombination(_stateInputCombinations[_selectedStateHash].combinations[_isPrimary], _action);
                    }



                    //					Debug.Log ("Action:"+_action+" "+_action.code);
                }


                //Debug.Log ("Action:"+action);
            }
            else
            {
               // UnityEngine.Debug.Log("Edit mode false");
                //Continue gameplay
                InputManager.EditMode = false;
            }
			

		}


		/// <summary>
		/// Saves the input settings.
		/// </summary>
		void saveInputSettings ()
		{
            #if UNITY_WEBPLAYER
                throw new NotImplementedException();//should be thru webserver side service
            #endif


            #if UNITY_STANDALONE
			            InputManager.saveSettings(Path.Combine(Application.streamingAssetsPath,settingsXML.name+".xml"));
            #endif
		}


		/// <summary>
		/// Loads the input settings.
		/// </summary>
		void loadInputSettings(){

			 //UnityEngine.Debug.Log("loadInputSettings");

			//clone(cos maybe some are added manually)
			_stateInputCombinations = new Dictionary<int,InputState> (InputManager.Settings.stateInputs);

            //load settngs from TextAsset(seem its utf-8 so not need of reading BOM)
            InputManager.loadSettingsFromText(settingsXML.text,false);

	
            var stateInputs = InputManager.Settings.stateInputs;

            //concat//concate with priority of keys/items loaded from .xml
            foreach (var KeyValuePair in _stateInputCombinations)
            {
                if (!stateInputs.ContainsKey(KeyValuePair.Key))
                    InputManager.Settings.stateInputs.Add(KeyValuePair.Key, KeyValuePair.Value);


            }

            //clone(cos maybe some are added manually)
            //_stateInputCombinations = new Dictionary<int, InputState>(InputManager.Settings.stateInputs);


			_stateInputCombinations=stateInputs;


		}


		/// <summary>
		/// Tos the input combination.
		/// </summary>
		/// <param name="combos">Combos.</param>
		/// <param name="input">Input.</param>
		void toInputCombination (InputCombination combos, InputAction input)
		{

			    if(combos.numActions+1 > maxCombosNum || (combos.numActions==1 && combos.GetActionAt(0).code==0))
				     combos.Clear();

					combos.Add(input);
					
			
		}

		/// <summary>
		/// Raises the GU event.
		/// </summary>
		private void OnGUI()
		{
			GUI.skin=guiSkin;

            GUI.Window(1, new Rect(0, 0, 600, 400), CreateWindow, new GUIContent());
			//GUI.Window(1, new Rect(0, 0, Screen.width, Screen.height), CreateWindow,new GUIContent());


			//if event is of key or mouse
			if (Event.current.isKey) {
				


				if (Event.current.keyCode == KeyCode.Return) {
					_selectedStateHash = 0;
					_previousStateInput = null;
					//this.Repaint ();
				} else
				if (Event.current.keyCode == KeyCode.Escape) {
					if (_selectedStateHash != 0) {
						_stateInputCombinations [_selectedStateHash].combinations[_isPrimary] = _previousStateInput;
						_previousStateInput = null;
						_selectedStateHash = 0;
					}
				}





			}

			//Approach dependent of GUI so not applicable if you have 3D GUI
			//if (_selectedStateHash != 0)
			//	InputEx.processGUIEvent (Event.current);//process input from keyboard & mouses
           
        }


		/// <summary>
		/// Creates the window.
		/// </summary>
		/// <param name="windowID">Window I.</param>
        private void CreateWindow(int windowID)
        {

			GUILayout.Label("http://unity3de.blogspot.com/");

			GUILayout.Label("InputEx");

				
				


					_scrollPosition=GUILayout.BeginScrollView(_scrollPosition,false,true);
			        
					

			if(_stateInputCombinations!=null)
			foreach (var keyValuPair in _stateInputCombinations)
			{
				//primary,secondary...
				createCombinationGUI(keyValuPair.Key,keyValuPair.Value.name,keyValuPair.Value.combinations);

			}
					
					GUILayout.EndScrollView ();
					
				
				
				
				GUILayout.Space(20);
				
				if(_selectedStateHash==0 && GUILayout.Button("Save")){
					saveInputSettings();
				}
				
			}




		/// <summary>
		/// Creates the combination GU.
		/// </summary>
		/// <param name="hash">Hash.</param>
		/// <param name="combinations">Combinations.</param>
		void createCombinationGUI (int hash,string stateName,InputCombination[] combinations)
		{

			string currentCombinationString;


			GUILayout.BeginHorizontal ();

			//string stateName=((CharacterInputControllerClass.States)hash).ToString();



				//(AnimatorEnum)hash
			//GUILayout.Label(stateName.Remove(0,stateName.IndexOf("Layer")+6).Replace("_"," "),_stateNameLabelStyle);

			
			
			if (_selectedStateHash != hash) {
				
				

				if (GUILayout.Button (combinations[0].combinationString)) {
					_selectedStateHash = hash;
					_previousStateInput = null;
					_isPrimary = 0;
				}

				if(combinations.Length>1 && combinations[1]!=null)
				if (GUILayout.Button (combinations[1].combinationString)) {
					_selectedStateHash = hash;
					_previousStateInput = null;
					_isPrimary = 1;
				}
				
				
			} else {

				currentCombinationString = combinations[_isPrimary].combinationString;

				if (_previousStateInput == null) {
					_previousStateInput = combinations[_isPrimary].Clone();
				}


				GUILayout.Label (currentCombinationString);//, _inputLabelStyle);

				//this.Repaint ();
			}
			
			
			
			//Debug.Log ("_selectedStateHash after" + _selectedStateHash);
			
			
			
			GUILayout.EndHorizontal ();
			
			
			
			GUILayout.Space(20);
		}

        
    }
}
