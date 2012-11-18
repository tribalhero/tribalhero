package src.Objects.Process 
{
	import adobe.utils.CustomActions;
	import src.Global;
	import src.Map.City;
	import src.Objects.GameObject;
	import src.UI.Dialog.AssignmentJoinAtkDialog;
	import src.UI.Dialog.AssignmentJoinDefDialog;

	public class AssignmentJoinProcess implements IProcess
	{		
		private var attackDialog: AssignmentJoinAtkDialog;
		private var reinforceDialog: AssignmentJoinDefDialog;
		private var assignment: * ;
		private var isAttack: Boolean;
		private var sourceCity:City;
		
		public function AssignmentJoinProcess(sourceCity: City, assignment: *) 
		{
			this.sourceCity = sourceCity;
			this.assignment = assignment;
			this.isAttack = assignment.isAttack==1;
		}
		
		public function execute(): void 
		{
			if(isAttack) {
				attackDialog = new AssignmentJoinAtkDialog(sourceCity, onChoseUnits, assignment)
				attackDialog.show();
			} else {
				reinforceDialog = new AssignmentJoinDefDialog(sourceCity, onChoseUnits, assignment)
				reinforceDialog.show();
			}
		}
		
		public function onChoseUnits(sender: *): void {				
			Global.mapComm.Troop.assignmentJoin(sourceCity.id, assignment.id, isAttack?attackDialog.getTroop():reinforceDialog.getTroop());
		}		
	}

}