using System.Collections.Generic;

/// <summary>
/// Лист связных списков(очередей) для поиска с приоритетом
/// </summary>
public class HexCellPriorityQueue
{

    List<HexCell> list = new List<HexCell>();

    int count = 0;//кол-во элементов в очереди

    int minimum = int.MaxValue;

    public int Count
    {
        get
        {
            return count;
        }
    }

    public void Enqueue(HexCell cell)
    {
        count += 1;
        int priority = cell.SearchPriority;
        if (priority < minimum)
        {
            minimum = priority;
        }
        while (priority >= list.Count)
        {
            list.Add(null);
        }
        cell.NextWithSamePriority = list[priority];
        list[priority] = cell;
    }

    /// <summary>
    /// Извлечение из списка ячейки с самым большим приоритетом
    /// Поиск идет с "минимума", чтобы не совершать лишних итераций 
    /// </summary>
    /// <returns></returns>
    public HexCell Dequeue()
    {
        count -= 1;
        for (; minimum < list.Count; minimum++)
        {
            HexCell cell = list[minimum];
            if (cell != null)
            {
                list[minimum] = cell.NextWithSamePriority;
                return cell;
            }
        }
        return null;
    }

    /// <summary>
    /// Изменить приритет ячейки
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="oldPriority"></param>
    public void Change(HexCell cell, int oldPriority)
    {
        HexCell current = list[oldPriority];
        HexCell next = current.NextWithSamePriority;
        if (current == cell)
        {
            list[oldPriority] = next;
        }
        else
        {
            while (next != cell)
            {
                current = next;
                next = current.NextWithSamePriority;
            }
        }
        current.NextWithSamePriority = cell.NextWithSamePriority;
        Enqueue(cell);
        count -= 1;
    }

    public void Clear()
    {
        list.Clear();
        count = 0;
        minimum = int.MaxValue;
    }
}