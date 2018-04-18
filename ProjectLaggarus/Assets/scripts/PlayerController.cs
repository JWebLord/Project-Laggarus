using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {

    /// <summary>
    /// Ссылка на текстовое поле для вывода текущего хода
    /// </summary>
    public Text currentTurnLabel;
    /// <summary>
    /// Числовое значение текущего хода
    /// </summary>
    private int currentTurn;
    /// <summary>
    /// Ссылка на грид этого игрока
    /// </summary>
    public HexGrid grid;

    /// <summary>
    /// Завершить ход(запуск процедур обсчета и всего, что необходимо)
    /// </summary>
    public void DoNextTurn()
    {
        currentTurn++;
        currentTurnLabel.text = "Current Turn: " + currentTurn.ToString();

        //Воссанавливаем очки передвижения юнитов
        foreach (HexUnit unit in grid.units)
        {
            DoUnitActions(unit);
            unit.RestoreUnitSpeed();
        }
    }

    private void DoUnitActions(HexUnit unit)
    {
        if (unit.currentDestinationCell && unit.currentDestinationCell != unit.Location)
        {
            grid.FindPath(unit.Location, unit.currentDestinationCell, unit, false);
            if (grid.HasPath)
            {
                unit.Travel(grid.GetPath());
                grid.ClearPath();
            }
        }
        else
        {
            unit.currentDestinationCell = null;
        }
    }

}
