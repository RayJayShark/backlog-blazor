using BacklogBlazor_Shared.Models;
using Microsoft.AspNetCore.Components;

namespace BacklogBlazor.Shared;

public partial class BacklogComponent : ComponentBase
{
    [Parameter]
    public BacklogModel Backlog { get; set; }
    
    [Parameter]
    public EventCallback EditClicked { get; set; }

    [Parameter] 
    public string Class { get; set; }
}