module GraphView

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Domain
open Model

/// D3 force simulation interop bindings
module D3 =
  /// Import D3 module
  [<Import("*", from = "d3")>]
  let d3 : obj = jsNative

  /// Select an element
  let select (selector : string) : obj = d3?select selector

  /// Select all elements
  let selectAll (selector : string) : obj = d3?selectAll selector

  /// Create a force simulation
  let forceSimulation (nodes : GraphNode array) : obj = d3?forceSimulation nodes

  /// Force: many-body (charge) force
  let forceManyBody () : obj = d3?forceManyBody ()

  /// Force: link force
  let forceLink (links : GraphLink array) : obj =
    let force = d3?forceLink (links)
    force?id (fun (d : GraphNode) -> d.Id) |> ignore
    force

  /// Force: centering force
  let forceCenter (x : float, y : float) : obj = d3?forceCenter (x, y)

  /// Force: collision force
  let forceCollide (radius : float) : obj = d3?forceCollide (radius)

  /// Create zoom behavior
  let zoom () : obj = d3?zoom ()

/// Convert backend Graph to frontend GraphData
let graphToGraphData (graph : Graph) : GraphData =
  let safeEdges = if isNull (box graph.Edges) then [] else graph.Edges
  let safeNodes = if isNull (box graph.Nodes) then [] else graph.Nodes

  let nodeDegrees =
    safeEdges
    |> List.fold
      (fun acc edge ->
        acc
        |> Map.change edge.Source (fun count -> Some((count |> Option.defaultValue 0) + 1))
        |> Map.change edge.Target (fun count -> Some((count |> Option.defaultValue 0) + 1)))
      Map.empty

  let nodes =
    safeNodes
    |> List.mapi (fun i id -> {
      Id = id
      Label = id
      Group = 0
      Degree = nodeDegrees |> Map.tryFind id |> Option.defaultValue 0
      X = None
      Y = None
      Vx = None
      Vy = None
    })

  let links =
    safeEdges
    |> List.map (fun edge -> { Source = edge.Source; Target = edge.Target; Value = 1.0 })

  { Nodes = nodes; Links = links }

/// Build a neighbor lookup map for fast neighbor checking
let buildNeighborMap (links : GraphLink list) : Map<string, Set<string>> =
  links
  |> List.fold
    (fun acc link ->
      acc
      |> Map.change link.Source (fun neighbors ->
        neighbors |> Option.defaultValue Set.empty |> Set.add link.Target |> Some)
      |> Map.change link.Target (fun neighbors ->
        neighbors |> Option.defaultValue Set.empty |> Set.add link.Source |> Some))
    Map.empty

/// Check if two nodes are neighbors
let areNeighbors (neighborMap : Map<string, Set<string>>) (nodeA : string) (nodeB : string) : bool =
  neighborMap
  |> Map.tryFind nodeA
  |> Option.map (fun neighbors -> neighbors.Contains nodeB)
  |> Option.defaultValue false

/// SVG graph view component
[<ReactComponent>]
let SvgGraph (state : State) (dispatch : Msg -> unit) =
  let graphData =
    match state.Graph with
    | Some graph -> graphToGraphData graph
    | None -> { Nodes = []; Links = [] }

  let width = 800.0
  let height = 600.0
  let neighborMap = buildNeighborMap graphData.Links
  let tooltipVisible, setTooltipVisible = React.useState false
  let tooltipContent, setTooltipContent = React.useState ""
  let tooltipX, setTooltipX = React.useState 0.0
  let tooltipY, setTooltipY = React.useState 0.0

  /// Determine if a node should be dimmed based on hover state
  let isNodeDimmed (nodeId : string) : bool =
    match state.HoveredNode with
    | Some hoveredId when hoveredId <> nodeId -> not (areNeighbors neighborMap hoveredId nodeId)
    | _ -> false

  /// Determine if a link should be dimmed or highlighted
  let getLinkClass (link : GraphLink) : string =
    match state.HoveredNode with
    | Some hoveredId when link.Source = hoveredId || link.Target = hoveredId -> "graph-link highlighted"
    | Some _ -> "graph-link dimmed"
    | None -> "graph-link"

  /// Get node CSS class based on state
  let getNodeClass (nodeId : string) : string =
    let baseClass = "graph-node"

    let selectedClass = if state.SelectedNode = Some nodeId then " selected" else ""

    let dimmedClass = if isNodeDimmed nodeId then " dimmed" else ""

    baseClass + selectedClass + dimmedClass

  /// Get label CSS class based on state
  let getLabelClass (nodeId : string) : string =
    let baseClass = "graph-label"

    let highlightClass =
      match state.HoveredNode with
      | Some hoveredId when hoveredId = nodeId || areNeighbors neighborMap hoveredId nodeId -> " highlighted"
      | _ -> ""

    let dimmedClass = if isNodeDimmed nodeId then " dimmed" else ""

    baseClass + highlightClass + dimmedClass

  React.useEffect (
    (fun () ->
      if not graphData.Nodes.IsEmpty then
        let nodes = Array.ofList graphData.Nodes
        let links = Array.ofList graphData.Links

        let svg = D3.select "#graph-svg"
        let zoomContainer = D3.select "#graph-zoom-container"
        let zoom = D3.zoom ()
        zoom?scaleExtent ([| 0.1; 10.0 |]) |> ignore

        zoom?on (
          "zoom",
          fun (event : obj) ->
            let transform = event?transform
            zoomContainer?attr ("transform", transform) |> ignore

            let scale = transform?k
            let tx = transform?x
            let ty = transform?y

            dispatch (GraphZoomChanged { Scale = scale; TranslateX = tx; TranslateY = ty })
        )
        |> ignore

        svg?call zoom |> ignore

        let simulation =
          D3.forceSimulation (nodes)
          |> fun sim ->
            sim?force ("charge", D3.forceManyBody () |> (fun f -> f?strength (-300.0)))
            |> ignore

            sim?force ("link", D3.forceLink (links) |> (fun f -> f?distance (100.0)))
            |> ignore

            sim?force ("center", D3.forceCenter (width / 2.0, height / 2.0)) |> ignore

            sim?force ("collide", D3.forceCollide (20.0)) |> ignore

            sim

        simulation?on (
          "tick",
          fun () ->
            zoomContainer?selectAll ".graph-link"
            |> fun links ->
              links?attr ("x1", fun (d : obj) -> d?source?x)
              |> fun l -> l?attr ("y1", fun (d : obj) -> d?source?y)
              |> fun l -> l?attr ("x2", fun (d : obj) -> d?target?x)
              |> fun l -> l?attr ("y2", fun (d : obj) -> d?target?y)
              |> ignore

            zoomContainer?selectAll ".graph-node"
            |> fun nodes ->
              nodes?attr ("cx", fun (d : GraphNode) -> d.X |> Option.defaultValue 0.0)
              |> fun n -> n?attr ("cy", fun (d : GraphNode) -> d.Y |> Option.defaultValue 0.0)
              |> ignore

            zoomContainer?selectAll ".graph-label"
            |> fun labels ->
              labels?attr ("x", fun (d : GraphNode) -> d.X |> Option.defaultValue 0.0)
              |> fun l -> l?attr ("y", fun (d : GraphNode) -> d.Y |> Option.defaultValue 0.0)
              |> ignore
        )
        |> ignore),
    [| box graphData.Nodes.Length |]
  )

  Html.div [
    prop.className "flex-1 flex flex-col bg-base00"
    prop.children [
      Html.div [
        prop.className "p-4 border-b border-base02"
        prop.children [
          Html.h1 [ prop.className "text-2xl font-bold text-base05"; prop.text "Graph View" ]
          Html.div [
            prop.className "text-sm text-base03 mt-1"
            prop.text $"{graphData.Nodes.Length} notes, {graphData.Links.Length} links"
          ]
          Html.div [
            prop.className "text-xs text-base03 mt-2"
            prop.children [
              Html.span [ prop.text "Scroll to zoom, drag to pan" ]
              Html.span [ prop.className "mx-2"; prop.text "•" ]
              Html.span [ prop.text $"Zoom: {state.ZoomState.Scale:F2}x" ]
            ]
          ]
        ]
      ]
      Html.div [
        prop.className "graph-container"
        prop.children [
          if graphData.Nodes.IsEmpty then
            Html.div [
              prop.className "text-center text-base03 absolute inset-0 flex items-center justify-center"
              prop.children [
                Html.div [
                  prop.children [
                    Html.p [ prop.text "No graph data available" ]
                    Html.button [
                      prop.className "mt-4 px-4 py-2 bg-blue hover:bg-blue-bright text-base00 rounded transition-all"
                      prop.text "Load Graph"
                      prop.onClick (fun _ -> dispatch LoadGraph)
                    ]
                  ]
                ]
              ]
            ]
          else
            React.fragment [
              Interop.reactApi.createElement (
                "svg",
                createObj [
                  "id" ==> "graph-svg"
                  "width" ==> width
                  "height" ==> height
                  "className" ==> "border border-base02"
                  "children"
                  ==> [|
                    Interop.reactApi.createElement (
                      "g",
                      createObj [
                        "id" ==> "graph-zoom-container"
                        "children"
                        ==> [|
                          for link in graphData.Links do
                            Interop.reactApi.createElement (
                              "line",
                              createObj [
                                "key" ==> $"{link.Source}-{link.Target}"
                                "className" ==> getLinkClass link
                                "stroke" ==> "#6b7089"
                                "strokeWidth" ==> 1.5
                                "data-source" ==> link.Source
                                "data-target" ==> link.Target
                              ]
                            )

                          for node in graphData.Nodes do
                            Interop.reactApi.createElement (
                              "circle",
                              createObj [
                                "key" ==> $"node-{node.Id}"
                                "className" ==> getNodeClass node.Id
                                "r" ==> (5.0 + float node.Degree)
                                "fill" ==> "#84a0c6"
                                "stroke" ==> "#c6c8d1"
                                "strokeWidth" ==> 1.5
                                "data-id" ==> node.Id
                                "onClick"
                                ==> fun _ ->
                                  dispatch (SelectNote node.Id)
                                  dispatch (NavigateTo(NoteEditor node.Id))
                                "onMouseEnter"
                                ==> fun (e : Browser.Types.MouseEvent) ->
                                  dispatch (GraphNodeHovered(Some node.Id))
                                  setTooltipContent node.Label
                                  setTooltipX e.clientX
                                  setTooltipY e.clientY
                                  setTooltipVisible true
                                "onMouseMove"
                                ==> fun (e : Browser.Types.MouseEvent) ->
                                  setTooltipX e.clientX
                                  setTooltipY e.clientY
                                "onMouseLeave"
                                ==> fun _ ->
                                  dispatch (GraphNodeHovered None)
                                  setTooltipVisible false
                              ]
                            )

                          for node in graphData.Nodes do
                            Interop.reactApi.createElement (
                              "text",
                              createObj [
                                "key" ==> $"label-{node.Id}"
                                "className" ==> getLabelClass node.Id
                                "fill" ==> "#c6c8d1"
                                "fontSize" ==> "10px"
                                "textAnchor" ==> "middle"
                                "dy" ==> "-10"
                                "children" ==> node.Label
                              ]
                            )
                        |]
                      ]
                    )
                  |]
                ]
              )

              if tooltipVisible then
                Html.div [
                  prop.className "graph-tooltip"
                  prop.style [ style.left (int (tooltipX + 10.0)); style.top (int (tooltipY + 10.0)) ]
                  prop.text tooltipContent
                ]
            ]
        ]
      ]
    ]
  ]

/// Canvas graph view component for large graphs
[<ReactComponent>]
let CanvasGraph (state : State) (dispatch : Msg -> unit) =
  let graphData =
    match state.Graph with
    | Some graph -> graphToGraphData graph
    | None -> { Nodes = []; Links = [] }

  let width = 800.0
  let height = 600.0

  let neighborMap = buildNeighborMap graphData.Links
  let tooltipVisible, setTooltipVisible = React.useState false
  let tooltipContent, setTooltipContent = React.useState ""
  let tooltipX, setTooltipX = React.useState 0.0
  let tooltipY, setTooltipY = React.useState 0.0

  let findNodeAtPosition (x : float) (y : float) (nodes : GraphNode list) : GraphNode option =
    nodes
    |> List.tryFind (fun node ->
      match node.X, node.Y with
      | Some nx, Some ny ->
        let dx = x - nx
        let dy = y - ny
        let radius = 5.0 + float node.Degree
        (dx * dx + dy * dy) <= (radius * radius)
      | _ -> false)

  /// Render the canvas graph
  let renderCanvas
    (canvas : Browser.Types.HTMLCanvasElement)
    (nodes : GraphNode array)
    (links : GraphLink array)
    (transform : ZoomState)
    =
    let ctx = canvas.getContext_2d ()
    ctx.clearRect (0.0, 0.0, width, height)
    ctx.save ()
    ctx.translate (transform.TranslateX, transform.TranslateY)
    ctx.scale (transform.Scale, transform.Scale)

    for link in links do
      let sourceNode = nodes |> Array.tryFind (fun n -> n.Id = link.Source)
      let targetNode = nodes |> Array.tryFind (fun n -> n.Id = link.Target)

      match sourceNode, targetNode with
      | Some source, Some target ->
        match source.X, source.Y, target.X, target.Y with
        | Some sx, Some sy, Some tx, Some ty ->
          let isHighlighted =
            match state.HoveredNode with
            | Some hoveredId when link.Source = hoveredId || link.Target = hoveredId -> true
            | _ -> false

          let isDimmed =
            match state.HoveredNode with
            | Some hoveredId when link.Source <> hoveredId && link.Target <> hoveredId -> true
            | _ -> false

          ctx.beginPath ()
          ctx.moveTo (sx, sy)
          ctx.lineTo (tx, ty)
          ctx.strokeStyle <- U3.Case1 "#6b7089"
          ctx.lineWidth <- if isHighlighted then 2.5 else 1.5

          ctx.globalAlpha <-
            if isDimmed then 0.1
            else if isHighlighted then 1.0
            else 0.6

          ctx.stroke ()
        | _ -> ()
      | _ -> ()

    ctx.globalAlpha <- 1.0

    for node in nodes do
      match node.X, node.Y with
      | Some x, Some y ->
        let isSelected = state.SelectedNode = Some node.Id
        let isHovered = state.HoveredNode = Some node.Id

        let isDimmed =
          match state.HoveredNode with
          | Some hoveredId when hoveredId <> node.Id -> not (areNeighbors neighborMap hoveredId node.Id)
          | _ -> false

        let radius = 5.0 + float node.Degree

        ctx.beginPath ()
        ctx.arc (x, y, radius, 0.0, 2.0 * System.Math.PI)

        if isSelected then
          ctx.fillStyle <- U3.Case1 "#91acd1"
          ctx.strokeStyle <- U3.Case1 "#e2a478"
        else if isHovered then
          ctx.fillStyle <- U3.Case1 "#84a0c6"
          ctx.strokeStyle <- U3.Case1 "#91acd1"
        else
          ctx.fillStyle <- U3.Case1 "#84a0c6"
          ctx.strokeStyle <- U3.Case1 "#c6c8d1"

        ctx.globalAlpha <- if isDimmed then 0.2 else 1.0
        ctx.fill ()
        ctx.lineWidth <- if isSelected || isHovered then 2.5 else 1.5
        ctx.stroke ()

        let isLabelHighlighted =
          match state.HoveredNode with
          | Some hoveredId when hoveredId = node.Id || areNeighbors neighborMap hoveredId node.Id -> true
          | _ -> false

        ctx.font <-
          if isLabelHighlighted then
            "bold 12px sans-serif"
          else
            "10px sans-serif"

        ctx.fillStyle <- U3.Case1(if isLabelHighlighted then "#91acd1" else "#c6c8d1")
        ctx.textAlign <- "center"
        ctx.globalAlpha <- if isDimmed then 0.2 else 1.0
        ctx.fillText (node.Label, x, y - radius - 5.0)
      | _ -> ()

    ctx.restore ()

  React.useEffect (
    (fun () ->
      if not graphData.Nodes.IsEmpty then
        let canvas = Browser.Dom.document.getElementById "graph-canvas"

        match canvas with
        | :? Browser.Types.HTMLCanvasElement as canvasElement ->
          let nodes = Array.ofList graphData.Nodes
          let links = Array.ofList graphData.Links

          let simulation =
            D3.forceSimulation (nodes)
            |> fun sim ->
              sim?force ("charge", D3.forceManyBody () |> (fun f -> f?strength (-300.0)))
              |> ignore

              sim?force ("link", D3.forceLink (links) |> (fun f -> f?distance (100.0)))
              |> ignore

              sim?force ("center", D3.forceCenter (width / 2.0, height / 2.0)) |> ignore

              sim?force ("collide", D3.forceCollide (20.0)) |> ignore

              sim

          simulation?on ("tick", fun () -> renderCanvas canvasElement nodes links state.ZoomState)
          |> ignore
        | _ -> ()),
    [|
      box graphData.Nodes.Length
      box state.HoveredNode
      box state.SelectedNode
      box state.ZoomState
    |]
  )

  Html.div [
    prop.className "flex-1 flex flex-col bg-base00"
    prop.children [
      Html.div [
        prop.className "p-4 border-b border-base02"
        prop.children [
          Html.h1 [ prop.className "text-2xl font-bold text-base05"; prop.text "Graph View" ]
          Html.div [
            prop.className "text-sm text-base03 mt-1"
            prop.text $"{graphData.Nodes.Length} notes, {graphData.Links.Length} links"
          ]
          Html.div [
            prop.className "text-xs text-base03 mt-2"
            prop.children [
              let currentEngine = if state.GraphEngine = Canvas then "Canvas" else "SVG"
              let targetEngine = if state.GraphEngine = Canvas then "SVG" else "Canvas"

              Html.span [ prop.text $"Rendering: {currentEngine}" ]
              Html.span [ prop.className "mx-2"; prop.text "•" ]

              Html.button [
                prop.className "text-blue hover:text-blue-bright default-transition cursor-pointer underline"
                prop.text $"Switch to {targetEngine}"
                prop.onClick (fun _ ->
                  dispatch (GraphEngineChanged(if state.GraphEngine = Canvas then Svg else Canvas)))
              ]
            ]
          ]
        ]
      ]
      Html.div [
        prop.className "graph-container"
        prop.children [
          if graphData.Nodes.IsEmpty then
            Html.div [
              prop.className "text-center text-base03 absolute inset-0 flex items-center justify-center"
              prop.children [
                Html.div [
                  prop.children [
                    Html.p [ prop.text "No graph data available" ]
                    Html.button [
                      prop.className "mt-4 px-4 py-2 bg-blue hover:bg-blue-bright text-base00 rounded transition-all"
                      prop.text "Load Graph"
                      prop.onClick (fun _ -> dispatch LoadGraph)
                    ]
                  ]
                ]
              ]
            ]
          else
            React.fragment [
              Html.canvas [
                prop.id "graph-canvas"
                prop.width width
                prop.height height
                prop.className "border border-base02"
                prop.onMouseMove (fun (e : Browser.Types.MouseEvent) ->
                  let canvas = e.currentTarget :?> Browser.Types.HTMLCanvasElement
                  let rect = canvas.getBoundingClientRect ()
                  let x = e.clientX - rect.left
                  let y = e.clientY - rect.top


                  let adjustedX = (x - state.ZoomState.TranslateX) / state.ZoomState.Scale
                  let adjustedY = (y - state.ZoomState.TranslateY) / state.ZoomState.Scale

                  match findNodeAtPosition adjustedX adjustedY graphData.Nodes with
                  | Some node ->
                    dispatch (GraphNodeHovered(Some node.Id))
                    setTooltipContent node.Label
                    setTooltipX e.clientX
                    setTooltipY e.clientY
                    setTooltipVisible true
                  | None ->
                    dispatch (GraphNodeHovered None)
                    setTooltipVisible false)
                prop.onMouseLeave (fun _ ->
                  dispatch (GraphNodeHovered None)
                  setTooltipVisible false)
                prop.onClick (fun (e : Browser.Types.MouseEvent) ->
                  let canvas = e.currentTarget :?> Browser.Types.HTMLCanvasElement
                  let rect = canvas.getBoundingClientRect ()
                  let x = e.clientX - rect.left
                  let y = e.clientY - rect.top


                  let adjustedX = (x - state.ZoomState.TranslateX) / state.ZoomState.Scale
                  let adjustedY = (y - state.ZoomState.TranslateY) / state.ZoomState.Scale

                  match findNodeAtPosition adjustedX adjustedY graphData.Nodes with
                  | Some node ->
                    dispatch (SelectNote node.Id)
                    dispatch (NavigateTo(NoteEditor node.Id))
                  | None -> ())
              ]

              if tooltipVisible then
                Html.div [
                  prop.className "graph-tooltip"
                  prop.style [ style.left (int (tooltipX + 10.0)); style.top (int (tooltipY + 10.0)) ]
                  prop.text tooltipContent
                ]
            ]
        ]
      ]
    ]
  ]

/// Render the graph view based on the current engine setting
let render (state : State) (dispatch : Msg -> unit) =
  match state.GraphEngine with
  | Svg -> SvgGraph state dispatch
  | Canvas -> CanvasGraph state dispatch
