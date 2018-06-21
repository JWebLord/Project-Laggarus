using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController {

    /// <summary>
    /// Числовое значение текущего хода
    /// </summary>
    public int currentTurn;

    public void DoTurn(HexGrid grid)
    {
        //Воссанавливаем очки передвижения юнитов
        foreach (HexUnit unit in grid.units)
        {
            DoUnitActions(grid, unit);
            unit.RestoreUnitSpeed();
        }

    }

    // TODO исправить поиск пути каждый ход для каждого юнита, либо сделать это заранее во время хода(чтобы не тратить время на обсчет ходов)
    private void DoUnitActions(HexGrid grid, HexUnit unit)
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
