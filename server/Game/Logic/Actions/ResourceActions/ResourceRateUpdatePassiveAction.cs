﻿#region

using System;
using System.Collections.Generic;
using Game.Data;
using Game.Logic.Procedures;
using Game.Setup;

#endregion

namespace Game.Logic.Actions
{
    public class ResourceRateUpdatePassiveAction : PassiveAction, IScriptable
    {
        private readonly Procedure procedure;

        private IStructure obj;

        public ResourceRateUpdatePassiveAction(Procedure procedure)
        {
            this.procedure = procedure;
        }

        public override void LoadProperties(IDictionary<string, string> properties)
        {
        }

        public override ActionType Type
        {
            get
            {
                return ActionType.ResourceRateUpdatePassive;
            }
        }

        public override string Properties
        {
            get
            {
                return string.Empty;
            }
        }

        #region IScriptable Members

        public void ScriptInit(IGameObject gameObject, string[] parms)
        {
            if ((obj = gameObject as IStructure) == null)
            {
                throw new Exception();
            }
            Execute();
        }

        #endregion

        public override Error Validate(string[] parms)
        {
            return Error.ActionNotFound;
        }

        public override Error Execute()
        {
            obj.City.BeginUpdate();
            procedure.RecalculateCityResourceRates(obj.City);
            obj.City.EndUpdate();
            return Error.Ok;
        }

        public override void UserCancelled()
        {
        }

        public override void WorkerRemoved(bool wasKilled)
        {
        }
    }
}