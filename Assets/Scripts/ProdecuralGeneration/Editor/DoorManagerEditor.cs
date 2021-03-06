using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(DoorManager))]
public class DoorManagerEditor : Editor {
	private DoorManager doorDefinitions;
	private SerializedObject serDoorDefinitions;
	private SerializedProperty serDoorList;
	//PrevSelectedDoor used for Name Editing, to disable textBox
	private DoorDefinition selectedDoor;
	private bool newDoorCreated = false;
	private GUIStyle buttonStyle;

	void OnEnable(){
		doorDefinitions = target as DoorManager;
	}

	void OnDisable(){
		SceneUpdater.HideGizmos = false;
		SceneUpdater.UpdateScene ();
	}

	public override void OnInspectorGUI(){

		buttonStyle = new GUIStyle ("button");
		buttonStyle.fontSize = 13;
		buttonStyle.fixedHeight = 18;

		if (SceneUpdater.IsActive) {
			serDoorDefinitions = new SerializedObject (doorDefinitions);
			serDoorList = serDoorDefinitions.FindProperty ("doors");
			int selectedIndex = doorDefinitions.doors.IndexOf (selectedDoor);
			SerializedProperty serPreviewDoors = serDoorDefinitions.FindProperty ("previewDoors");

			//PREVIEW DOOR toggle
			EditorGUILayout.Space ();
			string onOff = serPreviewDoors.boolValue ? ": On" : ": Off";

			EditorGUILayout.BeginHorizontal ();

			if (GUILayout.Button ("Mesh preview" + onOff, buttonStyle)) {
				bool prevPreviewVal = serPreviewDoors.boolValue;
				serPreviewDoors.boolValue = !serPreviewDoors.boolValue;
			}

			if (!SceneUpdater.HideGizmos) {
				EditorGUILayout.Space ();
				if (GUILayout.Button ("-> Enter edit mode", buttonStyle)) {
					SceneUpdater.HideGizmos = true;
				}
			}

			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.Space ();

			if (SceneUpdater.HideGizmos) {
				EditorGUILayout.Space ();
				//SerializedProperty serDoorDefSize = serDoorDefinitions.FindProperty ("doorSize");
				//float prevSize = serDoorDefSize.floatValue;
				//serDoorDefSize.floatValue = (float)EditorGUILayout.IntField ("Door size", (int)serDoorDefSize.floatValue);
				//doorDefinitions.AreDoorsDirty |= prevSize != serDoorDefSize.floatValue;
				//The doorsize should never be larger thant the actual room
				//serDoorDefSize.floatValue = Mathf.Clamp (serDoorDefSize.floatValue, 1f, doorDefinitions.AbstractBounds.minSize.y);
				//serDoorDefSize.floatValue = 2f;

				EditorGUILayout.Space ();
				SerializedProperty serMinCount = serDoorDefinitions.FindProperty ("minCount");
				SerializedProperty serMaxCount = serDoorDefinitions.FindProperty ("maxCount");
				serMinCount.intValue = EditorGUILayout.IntField ("Min Quantity", serMinCount.intValue);
				serMinCount.intValue = Mathf.Max (serMinCount.intValue, 1);
				serMinCount.intValue = Mathf.Min (serMinCount.intValue, serMaxCount.intValue);
				serMaxCount.intValue = EditorGUILayout.IntField ("Max Quantity", serMaxCount.intValue);
				serMaxCount.intValue = Mathf.Min (serMaxCount.intValue, serDoorList.arraySize);
				serMaxCount.intValue = Mathf.Max (serMaxCount.intValue, serMinCount.intValue);

				EditorGUILayout.Space ();

				for (int i = 0; i < serDoorList.arraySize; i++) {
					SerializedProperty serDoor = serDoorList.GetArrayElementAtIndex (i);
					SerializedProperty serDoorPos = serDoor.FindPropertyRelative ("Position");
					//SerializedProperty serDoorSize = serDoor.FindPropertyRelative ("Size");
					SerializedProperty serDoorName = serDoor.FindPropertyRelative ("Name");
					//serDoorSize.vector3Value = DoorDefinition.GlobalSize * Vector3.one;

					EditorGUILayout.BeginHorizontal ();

					GUIStyle labelStyle = EditorStyles.label;
					labelStyle.normal.textColor = (selectedIndex == i) ? Color.blue : Color.black;
					labelStyle.stretchWidth = true;
					string labelPrefix = serDoorPos.vector3Value.ToString ();

					if (doorDefinitions.doors.IndexOf (selectedDoor) == i) {
						EditorGUILayout.BeginHorizontal ();
						EditorGUILayout.PrefixLabel (labelPrefix, labelStyle);
						serDoorName.stringValue = EditorGUILayout.TextField (serDoorName.stringValue);
						EditorGUILayout.EndHorizontal ();
					} else {
						if (GUILayout.Button (labelPrefix, labelStyle)) {							
							selectedDoor = doorDefinitions.doors [i];
						}
						EditorGUILayout.LabelField (serDoorName.stringValue);
					}

					labelStyle.normal.textColor = Color.black;
					if (GUILayout.Button ("x", GUILayout.Width (20))) {
						serDoorList.DeleteArrayElementAtIndex (i);
						//In case the selected door has been deleted
						if (doorDefinitions.doors.IndexOf (selectedDoor) == i) {
							selectedDoor = null;
						}
					}
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.Space ();
				}

				EditorGUILayout.BeginHorizontal ();

				if (GUILayout.Button ("+ Add Door", buttonStyle)) {
					serDoorList.InsertArrayElementAtIndex (serDoorList.arraySize);
					InitializeNewDoor (serDoorList.GetArrayElementAtIndex (serDoorList.arraySize - 1));
				}

				if (selectedDoor != null && GUILayout.Button ("Clear Selection", buttonStyle)) {
					selectedDoor = null;
				}

				if (GUILayout.Button ("<- Leave edit mode", buttonStyle)) {
					SceneUpdater.HideGizmos = false;
				}

				EditorGUILayout.EndHorizontal ();
			}

			serDoorDefinitions.ApplyModifiedProperties ();
			SceneUpdater.UpdateScene ();
		}
	}

	//Initialized new doors before usage. Usually this could be done by a constructor, however,
	//The serializedObject used for the GUI doesn't call the constructor. It seems like it creates an exact
	//Copy of the last item in the array, which leads to problems.
	private void InitializeNewDoor(SerializedProperty doorDefinition){
		//SerializedProperty doorSize = doorDefinition.FindPropertyRelative ("Size");
		SerializedProperty doorCornerIndex = doorDefinition.FindPropertyRelative ("CornerIndex");
		SerializedProperty doorPosition = doorDefinition.FindPropertyRelative ("Position");
		SerializedProperty doorWorkingPosition = doorDefinition.FindPropertyRelative ("WorkingPosition");
		SerializedProperty doorOffset = doorDefinition.FindPropertyRelative ("Offset");
		SerializedProperty doorDirection = doorDefinition.FindPropertyRelative ("Direction");
		SerializedProperty doorID = doorDefinition.FindPropertyRelative ("ID");
		SerializedProperty doorName = doorDefinition.FindPropertyRelative ("Name");
		//doorSize.vector3Value = Vector3.one * DoorDefinition.GlobalSize;
		int defaultIndex = doorDefinitions.AbstractBounds.CornerIndicesByDirection (Vector3.forward) [1]; //Middle of the room;
		defaultIndex = doorDefinitions.AbstractBounds.ChangeCornersHeight(defaultIndex, doorDefinitions.GlobalYCornerHeight);
		doorCornerIndex.intValue = defaultIndex;
		doorPosition.vector3Value = doorDefinitions.AbstractBounds.Corners [doorCornerIndex.intValue];
		doorWorkingPosition.vector3Value = doorPosition.vector3Value;
		doorOffset.vector3Value = Vector3.up * doorDefinitions.GlobalYOffset;
		doorDirection.vector3Value = Vector3.forward;
		doorID.intValue = DoorManager.doorIndex++;
		doorName.stringValue = "Door " + doorID.intValue.ToString ();
		newDoorCreated = true;
	}

	public void OnSceneGUI(){
		if (SceneUpdater.IsActive) {
			//Repaint Inspector in order to update movement changes of the doors
			Repaint ();
			//Check if the selected object has been deleted
			if (doorDefinitions.doors.Count == 0) {
				selectedDoor = null;
			}
			//Autoselect new door
			//This will also result in initial clamping, see below
			if (newDoorCreated) {
				selectedDoor = doorDefinitions.doors [doorDefinitions.doors.Count - 1];
				newDoorCreated = false;
			}

			//Display doors as rectangle, color them red if selected
			//It seems like Handles have the limitation of having only floats as size input
			//Therefore I only support doors with same height / width, sadly
			foreach (DoorDefinition door in doorDefinitions.doors) {
				Handles.color = (door == selectedDoor) ? Color.red : Color.blue;
				if (Handles.Button (door.WorkingPosition, Quaternion.LookRotation(door.Direction), door.Extends.x, door.Extends.x, Handles.RectangleHandleCap)) {
					selectedDoor = door;
				}
				if (SceneUpdater.HideGizmos) {
					Handles.Label (door.Position, door.Name);
				}
			}

			//Handle logic for selected door
			if (selectedDoor == null) {
				doorDefinitions.DraggingDoor = null;
			} else {
				//Chewing gum effect. The handles are using the working position of the doors
				//Since also the offset is changed (below) the actual position of the door is bein calculated regardless
				//When releasing the mouse, set the working position to the current, calculated position
				//They may differ, since the working position is NOT aligned to the grid
				if (Event.current.type == EventType.MouseDown) {
					doorDefinitions.DraggingDoor = selectedDoor;
				}

				if (Event.current.type == EventType.MouseUp) {
					doorDefinitions.DraggingDoor = null;
					doorDefinitions.AdjustWorkingPosition (selectedDoor);
				}

				Vector3 horHandleDir = Vector3.Cross (Vector3.up, selectedDoor.Direction);

				if (horHandleDir.magnitude == 0f) {
					selectedDoor = null;
					return;
				}					

				float sliderSize = HandleUtility.GetHandleSize (selectedDoor.WorkingPosition) * 1.5f;
				//Draw Move Arrows, use Normal in order to point it at the right direction
				Handles.color = horHandleDir.normalized == Vector3.right || horHandleDir.normalized == Vector3.left ? Color.red : Color.blue;
				selectedDoor.WorkingPosition = Handles.Slider (selectedDoor.WorkingPosition, horHandleDir, selectedDoor.Extends.x * sliderSize, Handles.ArrowHandleCap, 1f);
				Handles.color = Color.green;
				selectedDoor.WorkingPosition = Handles.Slider (selectedDoor.WorkingPosition, Vector3.up, selectedDoor.Extends.y * sliderSize, Handles.ArrowHandleCap, 1f);
				//Eventhough the position is already being clamped in the preview function of doorsPositions,
				//This clamp is important since several frames may pass before the next call of preview
				doorDefinitions.ClampPosition (selectedDoor);
				//Calculate Offset to the selected corner. Essential for docking
				//DoorDefinitions later calculates the position from the corner pos and offset
				selectedDoor.Offset = selectedDoor.WorkingPosition - doorDefinitions.AbstractBounds.Corners[selectedDoor.CornerIndex];
				doorDefinitions.UpdateYOffset (selectedDoor.Offset.y);
				//Uniform handle size factor
				float directionHandleSize = HandleUtility.GetHandleSize (selectedDoor.WorkingPosition) * 0.8f;
				//Get one of four directions. The direction represent the wall the door is locked to.
				Vector3 newDirection = EditorGUIExtension.DirectionHandleVec (selectedDoor.WorkingPosition, directionHandleSize, selectedDoor.Direction, new Vector3(1f, 0f, 1f));
				if (newDirection != selectedDoor.Direction) {
					selectedDoor.Direction = newDirection;
					//Default docking corner is at index one, bottom middle. See AbstractBounds for corner indices.
					selectedDoor.CornerIndex = doorDefinitions.AbstractBounds.CornerIndicesByDirection (newDirection) [1];
					selectedDoor.Position = doorDefinitions.AbstractBounds.Corners [selectedDoor.CornerIndex];
					doorDefinitions.ClampPosition (selectedDoor);
					doorDefinitions.AdjustWorkingPosition (selectedDoor);
				}

				//Retrieve all corner positions belonging to a certain wall defined by a direction vector
				int[] indices = doorDefinitions.AbstractBounds.CornerIndicesByDirection (selectedDoor.Direction);
				//Draw docking buttons
				foreach (int i in indices) {
					float dockHandleSize = HandleUtility.GetHandleSize (doorDefinitions.AbstractBounds.Corners[i]) * 0.1f;
					if (Handles.Button (doorDefinitions.AbstractBounds.Corners [i], Quaternion.identity, dockHandleSize, dockHandleSize, Handles.DotHandleCap)) {
						selectedDoor.Position = doorDefinitions.AbstractBounds.Corners [i];
						selectedDoor.Offset = Vector2.up * doorDefinitions.GlobalYOffset;
						selectedDoor.CornerIndex = i;
						ApplyCornerHeight (i);
						doorDefinitions.AdjustWorkingPosition (selectedDoor);
					}
				}					
			}
		}
	}
		
	private void ApplyCornerHeight(int index){
		AbstractBounds ab = doorDefinitions.AbstractBounds;
		int targetHeight = ab.CornersHeightDegree (index);
		doorDefinitions.GlobalYCornerHeight = targetHeight;
		foreach (DoorDefinition door in doorDefinitions.doors) {
			int newCorner = ab.ChangeCornersHeight (door.CornerIndex, targetHeight);
			door.CornerIndex = newCorner;
			door.Position.y = ab.Corners [newCorner].y;
			door.Offset.y = 0f;
			doorDefinitions.AdjustWorkingPosition (door);
		}
		doorDefinitions.Preview ();
	}
}
