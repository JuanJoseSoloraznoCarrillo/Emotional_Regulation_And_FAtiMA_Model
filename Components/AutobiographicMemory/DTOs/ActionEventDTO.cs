﻿using System;

namespace AutobiographicMemory.DTOs
{
    /// <summary>
    /// Data Type Object Class for the representation of an Event referent to an action execution
    /// </summary>
    [Serializable]
    public class ActionEventDTO : EventDTO
    {
		public ActionState ActionState { get; set; }

		/// <summary>
		/// The action referent to this event.
		/// </summary>
        public string Action { get; set; }

		/// <summary>
		/// The target of action to which this event refers.
		/// </summary>
		public string Target { get; set; }
    }

	public enum ActionState : byte
	{
		Start,
		Finished
	}
}
