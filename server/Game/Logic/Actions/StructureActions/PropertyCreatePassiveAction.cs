#region

using System;
using System.Collections.Generic;
using Game.Data;
using Game.Setup;
using Game.Util;
using Game.Util.Locking;
using Persistance;

#endregion

namespace Game.Logic.Actions
{
    public class PropertyCreatePassiveAction : PassiveAction, IScriptable
    {
        private string name;

        private IStructure structure;

        private object value;

        private readonly ILocker locker;

        public PropertyCreatePassiveAction(ILocker locker)
        {
            this.locker = locker;
        }

        public override void LoadProperties(IDictionary<string, string> properties)
        {
        }

        public override ActionType Type
        {
            get
            {
                return ActionType.PropertyCreatePassive;
            }
        }

        public override string Properties
        {
            get
            {
                return XmlSerializer.Serialize(new[] {new XmlKvPair("value", (uint)value),});
            }
        }

        public void ScriptInit(IGameObject obj, string[] parms)
        {
            if ((structure = obj as IStructure) == null)
            {
                throw new Exception();
            }
            name = parms[0];

            switch(parms[1].ToLower())
            {
                case "byte":
                    value = byte.Parse(parms[2]);
                    break;
                case "ushort":
                    value = ushort.Parse(parms[2]);
                    break;
                case "int":
                    value = int.Parse(parms[2]);
                    break;
                case "uint":
                    value = uint.Parse(parms[2]);
                    break;
                case "string":
                    value = parms[2];
                    break;
                case "randomint":
                    value = Config.Random.Next(int.Parse(parms[2]), int.Parse(parms[3]));
                    break;
                default:
                    throw new Exception("Type not supported for structure property");
            }

            Execute();
        }

        public override Error Validate(string[] parms)
        {
            return Error.Ok;
        }

        public override Error Execute()
        {
            structure.BeginUpdate();
            structure[name] = value;
            structure.EndUpdate();
            
            StateChange(ActionState.Completed);

            return Error.Ok;
        }

        public override void WorkerRemoved(bool wasKilled)
        {
            locker.Lock(structure.City).Do(() =>
            {
                if (!IsValid())
                {
                    return;
                }

                StateChange(ActionState.Failed);
            });
        }

        public override void UserCancelled()
        {
        }
    }
}