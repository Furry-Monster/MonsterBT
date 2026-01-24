# MonsterBT Editor 架构文档

## 1. 整体架构图

```mermaid
graph TB
    subgraph "Editor Window Layer"
        BTEditorWindow[BTEditorWindow<br/>主窗口容器]
    end
    
    subgraph "UI Module Layer"
        BTToolbar[BTToolbar<br/>工具栏]
        BTNodeGraphView[BTNodeGraphView<br/>图形视图]
        BTNodeLibrary[BTNodeLibrary<br/>节点库]
        BTNodeInspector[BTNodeInspector<br/>属性检查器]
        BTStatusBar[BTStatusBar<br/>状态栏]
        BTNodeView[BTNodeView<br/>节点视图]
    end
    
    subgraph "Service Layer"
        BTEditorEventBus[BTEditorEventBus<br/>事件总线]
        BTNodeOperationService[BTNodeOperationService<br/>节点操作服务]
        BTBlackboardViewManager[BTBlackboardViewManager<br/>黑板视图管理]
        BTGraphCommandHandler[BTGraphCommandHandler<br/>命令处理]
        BTGraphContextMenuBuilder[BTGraphContextMenuBuilder<br/>上下文菜单构建]
        BTNodeEditorService[BTNodeEditorService<br/>节点编辑服务]
        BTFieldEditorService[BTFieldEditorService<br/>字段编辑服务]
        BTBehaviorTreeService[BTBehaviorTreeService<br/>行为树服务]
        BTAssetService[BTAssetService<br/>资源服务]
        BTEditorAssetService[BTEditorAssetService<br/>编辑器资源服务]
    end
    
    subgraph "Utils Layer"
        BTNodeTypeHelper[BTNodeTypeHelper<br/>节点类型助手]
        BTEditorResources[BTEditorResources<br/>资源加载器]
    end
    
    subgraph "Runtime Layer"
        BehaviorTree[BehaviorTree<br/>行为树资产]
        BTNode[BTNode<br/>节点基类]
        Blackboard[Blackboard<br/>黑板]
    end
    
    BTEditorWindow --> BTToolbar
    BTEditorWindow --> BTNodeGraphView
    BTEditorWindow --> BTNodeLibrary
    BTEditorWindow --> BTNodeInspector
    BTEditorWindow --> BTStatusBar
    
    BTNodeGraphView --> BTNodeView
    BTNodeGraphView --> BTNodeOperationService
    BTNodeGraphView --> BTBlackboardViewManager
    BTNodeGraphView --> BTGraphCommandHandler
    BTNodeGraphView --> BTGraphContextMenuBuilder
    
    BTNodeInspector --> BTFieldEditorService
    BTNodeLibrary --> BTNodeTypeHelper
    
    BTEditorEventBus -.->|事件通信| BTEditorWindow
    BTEditorEventBus -.->|事件通信| BTNodeGraphView
    BTEditorEventBus -.->|事件通信| BTNodeInspector
    BTEditorEventBus -.->|事件通信| BTStatusBar
    
    BTNodeOperationService --> BTNodeEditorService
    BTNodeOperationService --> BTEditorAssetService
    BTGraphCommandHandler --> BTNodeOperationService
    BTGraphContextMenuBuilder --> BTNodeOperationService
    
    BTBehaviorTreeService --> BTAssetService
    BTBlackboardViewManager --> BTEditorAssetService
    
    BTNodeGraphView --> BehaviorTree
    BTNodeView --> BTNode
    BTBlackboardViewManager --> Blackboard
    
    style BTEditorWindow fill:#e1f5ff
    style BTEditorEventBus fill:#fff4e1
    style BTNodeOperationService fill:#e8f5e9
    style BehaviorTree fill:#fce4ec
```

## 2. 类关系图

```mermaid
classDiagram
    class BTEditorWindow {
        -BTNodeGraphView graphView
        -BTNodeInspector inspector
        -BTNodeLibrary nodeLibrary
        -BTToolbar toolbar
        -BTStatusBar statusBar
        +SetBehaviorTree(BehaviorTree)
        +CreateGUI()
    }
    
    class BTNodeGraphView {
        -BehaviorTree behaviorTree
        -Dictionary~BTNode,BTNodeView~ nodeViews
        -BTBlackboardViewManager blackboardManager
        -BTNodeOperationService nodeOperationService
        -BTGraphCommandHandler commandHandler
        -BTGraphContextMenuBuilder contextMenuBuilder
        +SetBehaviorTree(BehaviorTree)
        +PopulateView()
        +Create(Type, Vector2)
    }
    
    class BTNodeView {
        +BTNode Node
        +Port InputPort
        +Port OutputPort
        +RefreshContent(string)
    }
    
    class BTNodeOperationService {
        +CreateNode(Type, Vector2, Func)
        +DeleteNode(BTNodeView)
        +DuplicateNode(BTNodeView, Vector2, Func)
        +PasteNode(BTNode, Vector2, Func)
    }
    
    class BTBlackboardViewManager {
        -BehaviorTree behaviorTree
        -Blackboard blackboardView
        +RefreshView()
        +AddVariable(string, Type)
        +RemoveVariable(string)
        +RenameVariable(string, string)
    }
    
    class BTGraphCommandHandler {
        +HandleExecuteCommand(ExecuteCommandEvent)
        +HandleValidateCommand(ValidateCommandEvent)
    }
    
    class BTGraphContextMenuBuilder {
        +BuildContextualMenu(ContextualMenuPopulateEvent, Vector2)
    }
    
    class BTEditorEventBus {
        <<static>>
        +OnBehaviorTreeChanged
        +OnNodeSelected
        +OnNodeDeselected
        +OnPropertyChanged
        +OnLogMessage
        +PublishBehaviorTreeChanged(BehaviorTree)
        +PublishNodeSelected(BTNode)
    }
    
    class BTNodeEditorService {
        <<static>>
        +GetChildren(BTNode) IEnumerable
        +SetChild(BTNode, BTNode)
        +RemoveChild(BTNode, BTNode)
        +CanDeleteNode(BTNode) bool
        +CanCopyNode(BTNode, BehaviorTree) bool
    }
    
    class BTFieldEditorService {
        <<static>>
        +CreateStringField(FieldInfo, BTNode, string)
        +CreateFloatField(FieldInfo, BTNode, string)
        +CreateBoolField(FieldInfo, BTNode, string)
        +CreateComponentField(FieldInfo, BTNode, string)
    }
    
    BTEditorWindow --> BTNodeGraphView
    BTEditorWindow --> BTNodeInspector
    BTEditorWindow --> BTNodeLibrary
    BTEditorWindow --> BTToolbar
    BTEditorWindow --> BTStatusBar
    BTEditorWindow ..> BTEditorEventBus : subscribes
    
    BTNodeGraphView --> BTNodeView
    BTNodeGraphView --> BTNodeOperationService
    BTNodeGraphView --> BTBlackboardViewManager
    BTNodeGraphView --> BTGraphCommandHandler
    BTNodeGraphView --> BTGraphContextMenuBuilder
    BTNodeGraphView ..> BTEditorEventBus : publishes/subscribes
    
    BTNodeView --> BTNode : references
    
    BTNodeOperationService --> BTNodeEditorService
    BTNodeOperationService --> BTEditorAssetService
    
    BTGraphCommandHandler --> BTNodeOperationService
    BTGraphContextMenuBuilder --> BTNodeOperationService
    
    BTNodeInspector --> BTFieldEditorService
    BTNodeInspector ..> BTEditorEventBus : subscribes
```

## 3. 数据流和事件流图

```mermaid
sequenceDiagram
    participant User as 用户操作
    participant Toolbar as BTToolbar
    participant GraphView as BTNodeGraphView
    participant EventBus as BTEditorEventBus
    participant Inspector as BTNodeInspector
    participant StatusBar as BTStatusBar
    participant Service as BTNodeOperationService
    
    User->>Toolbar: 选择 BehaviorTree
    Toolbar->>EventBus: PublishBehaviorTreeChanged
    EventBus->>GraphView: OnBehaviorTreeChanged
    GraphView->>GraphView: SetBehaviorTree
    GraphView->>GraphView: PopulateView
    
    User->>GraphView: 点击节点
    GraphView->>EventBus: PublishNodeSelected
    EventBus->>Inspector: OnNodeSelected
    Inspector->>Inspector: SetSelectedNode
    Inspector->>Inspector: RefreshInspector
    
    User->>Inspector: 修改属性
    Inspector->>EventBus: PublishPropertyChanged
    EventBus->>GraphView: OnPropertyChanged
    GraphView->>GraphView: UpdateNodeContent
    
    User->>GraphView: 创建节点
    GraphView->>Service: CreateNode
    Service->>Service: CreateNodeInAsset
    Service->>GraphView: CreateViewForNode
    GraphView->>GraphView: AddElement
    
    User->>GraphView: 删除节点
    GraphView->>Service: DeleteNode
    Service->>Service: RemoveNodeFromGraph
    Service->>GraphView: RemoveElement
    
    Runtime->>StatusBar: Log Message
    StatusBar->>EventBus: PublishLogMessage
    EventBus->>StatusBar: OnLogMessage
    StatusBar->>StatusBar: SetStatus
```

## 4. 服务层架构图

```mermaid
graph LR
    subgraph "Operation Services"
        BTNodeOperationService[BTNodeOperationService<br/>节点CRUD操作]
        BTNodeEditorService[BTNodeEditorService<br/>节点关系管理]
    end
    
    subgraph "View Services"
        BTBlackboardViewManager[BTBlackboardViewManager<br/>黑板视图管理]
        BTGraphCommandHandler[BTGraphCommandHandler<br/>快捷键命令]
        BTGraphContextMenuBuilder[BTGraphContextMenuBuilder<br/>右键菜单]
    end
    
    subgraph "Asset Services"
        BTBehaviorTreeService[BTBehaviorTreeService<br/>行为树创建/保存]
        BTAssetService[BTAssetService<br/>资源验证/修复]
        BTEditorAssetService[BTEditorAssetService<br/>资源标记]
    end
    
    subgraph "Field Services"
        BTFieldEditorService[BTFieldEditorService<br/>字段编辑器创建]
    end
    
    subgraph "Event System"
        BTEditorEventBus[BTEditorEventBus<br/>事件总线]
    end
    
    BTNodeOperationService --> BTNodeEditorService
    BTNodeOperationService --> BTEditorAssetService
    BTGraphCommandHandler --> BTNodeOperationService
    BTGraphContextMenuBuilder --> BTNodeOperationService
    BTBlackboardViewManager --> BTEditorAssetService
    BTBehaviorTreeService --> BTAssetService
    
    BTEditorEventBus -.->|解耦通信| BTNodeOperationService
    BTEditorEventBus -.->|解耦通信| BTBlackboardViewManager
    BTEditorEventBus -.->|解耦通信| BTBehaviorTreeService
    
    style BTNodeOperationService fill:#e8f5e9
    style BTEditorEventBus fill:#fff4e1
    style BTBehaviorTreeService fill:#e1f5ff
```

## 5. UI 组件层次结构

```mermaid
graph TD
    BTEditorWindow[BTEditorWindow<br/>EditorWindow]
    
    BTEditorWindow --> ToolbarContainer[BTToolbar<br/>工具栏容器]
    BTEditorWindow --> MainContent[TwoPaneSplitView<br/>主内容区]
    BTEditorWindow --> StatusBarContainer[BTStatusBar<br/>状态栏]
    
    MainContent --> LeftPanel[BTNodeLibrary<br/>节点库面板]
    MainContent --> RightSplit[TwoPaneSplitView<br/>右侧分割]
    
    RightSplit --> CenterPanel[BTNodeGraphView<br/>图形视图]
    RightSplit --> InspectorPanel[BTNodeInspector<br/>属性检查器]
    
    CenterPanel --> BlackboardView[Blackboard<br/>Unity GraphView Blackboard]
    CenterPanel --> MiniMap[MiniMap<br/>小地图]
    CenterPanel --> GridBackground[GridBackground<br/>网格背景]
    CenterPanel --> NodeViews[BTNodeView[]<br/>节点视图集合]
    
    NodeViews --> NodeView1[BTNodeView<br/>节点1]
    NodeViews --> NodeView2[BTNodeView<br/>节点2]
    NodeViews --> NodeViewN[BTNodeView<br/>节点N]
    
    style BTEditorWindow fill:#e1f5ff
    style CenterPanel fill:#e8f5e9
    style InspectorPanel fill:#fff4e1
```

## 6. 职责分离架构

```mermaid
graph TB
    subgraph "Presentation Layer 表现层"
        UI[UI Components<br/>BTNodeGraphView<br/>BTNodeInspector<br/>BTNodeLibrary<br/>BTToolbar<br/>BTStatusBar]
    end
    
    subgraph "Service Layer 服务层"
        Operation[Operation Services<br/>BTNodeOperationService<br/>BTNodeEditorService]
        
        View[View Services<br/>BTBlackboardViewManager<br/>BTGraphCommandHandler<br/>BTGraphContextMenuBuilder]
        
        Asset[Asset Services<br/>BTBehaviorTreeService<br/>BTAssetService<br/>BTEditorAssetService]
        
        Field[Field Services<br/>BTFieldEditorService]
    end
    
    subgraph "Infrastructure Layer 基础设施层"
        Event[Event System<br/>BTEditorEventBus]
        Utils[Utilities<br/>BTNodeTypeHelper<br/>BTEditorResources]
    end
    
    subgraph "Data Layer 数据层"
        Runtime[Runtime Assets<br/>BehaviorTree<br/>BTNode<br/>Blackboard]
    end
    
    UI --> Operation
    UI --> View
    UI --> Asset
    UI --> Field
    UI -.->|事件| Event
    UI --> Utils
    
    Operation --> Asset
    Operation --> Runtime
    View --> Asset
    Asset --> Runtime
    
    Event -.->|解耦| UI
    Event -.->|解耦| Operation
    Event -.->|解耦| View
    
    style UI fill:#e1f5ff
    style Operation fill:#e8f5e9
    style View fill:#e8f5e9
    style Asset fill:#e8f5e9
    style Event fill:#fff4e1
    style Runtime fill:#fce4ec
```

## 架构特点

### 1. 分层架构
- **表现层**: UI 组件，负责用户交互和视觉呈现
- **服务层**: 业务逻辑处理，职责单一
- **基础设施层**: 事件系统和工具类
- **数据层**: Runtime 资产

### 2. 职责分离
- **UI 组件**: 只负责显示和用户交互
- **服务类**: 处理业务逻辑和资产操作
- **事件总线**: 实现组件间解耦通信

### 3. 可扩展性
- 新节点类型自动发现和分类
- 新字段类型通过 `BTFieldEditorService` 扩展
- 新功能通过服务类添加，无需修改 UI

### 4. 设计模式
- **服务定位器模式**: 服务类集中管理
- **观察者模式**: 事件总线实现事件通信
- **策略模式**: 不同类型的字段编辑器
- 工厂模式: 节点和视图的创建
