﻿using UnityEngine;
using System;
using System.Collections.Generic;

namespace Hsm {

	[System.Serializable]
	public class StateMachine {
		[SerializeField]
		public List<State> states = new List<State>();
		[SerializeField]
		public State initialState;
		[SerializeField]
		public State currentState;

		private bool eventInProgress = false;

		// For storing incoming events.
		struct Event {
			public string evt;
			public Dictionary<string, object> data;

			public Event(string evt, Dictionary<string, object> data) {
				this.evt = evt;
				this.data = data;
			}
		}
		private Queue<Event> eventQueue = new Queue<Event>();

		public StateMachine(List<State> pStates) {
			states = pStates;
		}

		public StateMachine(params State[] pStates) {
			states.AddRange(pStates);
		}

		public void setup() {
			if (states.Count == 0) {
				throw new UnityException("StateMachine.setup: Must have states!");
			}
			if (initialState == null) {
				initialState = states[0];
			}
			_enterState(null, initialState, new Dictionary<string, object>());
		}

		public void tearDown(State nextState) {
			currentState.Exit(nextState);
			currentState = null;
		}

		public StateMachine addState(State pState) {
			// TODO: Check if state with id already exists
			states.Add(pState);
			return this;
		}

		public void handleEvent(string evt) {
			handleEvent(evt, new Dictionary<string, object>());
		}

		public void handleEvent(string evt, Dictionary<string, object> data) {
			Event myEvent = new Event(evt, data);
			eventQueue.Enqueue(myEvent);
			if (eventInProgress == true) {
				// EnQueue
			} else {
				// DeQueue
				eventInProgress = true;
				Event curEvent;
				while (eventQueue.Count > 0) {
					curEvent = eventQueue.Dequeue();
					Handle(curEvent.evt, curEvent.data);
				}
				eventInProgress = false;
			}
		}

		public bool Handle(string evt, Dictionary<string, object> data) {
			// check if current state is a (nested) statemachine, if so, give it the event.
			// if it handles the event, stop processing here.
			if (currentState is INestedState) {
				INestedState nested = currentState as INestedState;
				if (nested.Handle(evt, data)) {
					return true;
				}
			}
			
			if (!currentState.handlers.ContainsKey(evt)) {
				return false;
			}
			
			List<Handler> handlers = currentState.handlers[evt];
			foreach (Handler handler in handlers) {
				if (_performTransition(handler, data)) {
					return true;
				}
			}
			
			return false;
		}

		private bool _performTransition(Handler handler, Dictionary<string, object> data) {
			if (handler.kind == TransitionKind.Internal) {
				return _performInternalTransition(handler, data);
			} else {
				return _performExternalTransition(handler, data);
			}
		}

		private bool _performExternalTransition(Handler handler, Dictionary<string, object> data) {
			if (handler.target == null) {
				return false;
			}
			_switchState(currentState, handler.target, handler.action, data);
			return true;
		}

		private bool _performInternalTransition(Handler handler, Dictionary<string, object> data) {
			if (handler.action != null) {
				handler.action.Invoke(data);
			}
			return true;
		}

		private void _switchState(State sourceState, State targetState, Action<Dictionary<string, object>> action, Dictionary<string, object> data) {
			sourceState.Exit(targetState);
			if (action != null) {
				action.Invoke(data);
			}
			_enterState(sourceState, targetState, data);
		}

		public void _enterState(State sourceState, State targetState, Dictionary<string, object> data) {
			currentState = targetState;
			targetState.Enter(sourceState, targetState, data);
		}

	}
}

