using System.Globalization;
using FHOOE.Freydis.Agents.Support.Logging;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;

namespace FHOOE.Freydis.Agents.Agents.Kuka.Services;

/// <summary>
///     Reads data from KUKA iiwa 14 robot via OPC UA server.
///     Handles joint values, torque values, and execution time estimates.
/// </summary>
public sealed class KukaIiwa14DataReader
{
    private const string RobotNodePath = "KUKA iiwa 14";
    private const int NamespaceIndex = 2;
    private const int NumberOfAxes = 7;
    private readonly ILogger<KukaIiwa14DataReader> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KukaIiwa14DataReader" /> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public KukaIiwa14DataReader(ILogger<KukaIiwa14DataReader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Asynchronously reads all 7 joint angle values from the KUKA iiwa 14 robot.
    /// </summary>
    /// <param name="session">The connected OPC UA session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An array of 7 joint values in radians, or null if reading fails.</returns>
    public async Task<double[]?> ReadJointValuesAsync(
        ISession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!session.Connected)
        {
            _logger.LogDataReaderCannotReadJointValuesNotConnected();
            return null;
        }

        try
        {
            var nodesToRead = new ReadValueIdCollection();
            for (var i = 0; i < NumberOfAxes; i++)
                nodesToRead.Add(new ReadValueId
                {
                    NodeId = new NodeId($"ns={NamespaceIndex};s={RobotNodePath}.JointValues.A{i + 1}"),
                    AttributeId = Attributes.Value
                });

            DataValueCollection? results = null;

            await Task.Run(() =>
                    session.Read(null, 0, TimestampsToReturn.Neither, nodesToRead, out results, out _),
                cancellationToken);

            if (results == null)
            {
                _logger.LogJointValuesNullResults();
                return null;
            }

            var jointValues = new double[NumberOfAxes];
            for (var i = 0; i < NumberOfAxes; i++)
                if (StatusCode.IsGood(results[i].StatusCode))
                {
                    jointValues[i] = Convert.ToDouble(results[i].Value, CultureInfo.InvariantCulture);
                }
                else
                {
                    _logger.LogJointReadFailed(i + 1, results[i].StatusCode);
                    return null;
                }

            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogJointValuesRead(string.Join(", ", jointValues));
            return jointValues;
        }
        catch (Exception ex)
        {
            _logger.LogJointValuesReadError(ex);
            return null;
        }
    }

    /// <summary>
    ///     Asynchronously reads all 7 joint torque values from the KUKA iiwa 14 robot.
    /// </summary>
    /// <param name="session">The connected OPC UA session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An array of 7 torque values in Nm, or null if reading fails.</returns>
    public async Task<double[]?> ReadTorqueValuesAsync(
        ISession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!session.Connected)
        {
            _logger.LogDataReaderCannotReadTorqueValuesNotConnected();
            return null;
        }

        try
        {
            var nodesToRead = new ReadValueIdCollection();
            for (var i = 0; i < NumberOfAxes; i++)
                nodesToRead.Add(new ReadValueId
                {
                    NodeId = new NodeId($"ns={NamespaceIndex};s={RobotNodePath}.TorqueValues.T{i + 1}"),
                    AttributeId = Attributes.Value
                });

            DataValueCollection? results = null;

            await Task.Run(() =>
                    session.Read(null, 0, TimestampsToReturn.Neither, nodesToRead, out results, out _),
                cancellationToken);

            if (results == null)
            {
                _logger.LogTorqueValuesNullResults();
                return null;
            }

            var torqueValues = new double[NumberOfAxes];
            for (var i = 0; i < NumberOfAxes; i++)
                if (StatusCode.IsGood(results[i].StatusCode))
                {
                    torqueValues[i] = Convert.ToDouble(results[i].Value, CultureInfo.InvariantCulture);
                }
                else
                {
                    _logger.LogTorqueReadFailed(i + 1, results[i].StatusCode);
                    return null;
                }

            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTorqueValuesRead(string.Join(", ", torqueValues));
            return torqueValues;
        }
        catch (Exception ex)
        {
            _logger.LogTorqueValuesReadError(ex);
            return null;
        }
    }

    /// <summary>
    ///     Calls the GetExecutionEstimateAsync method on the OPC UA server to estimate skill execution time.
    ///     This method calculates the estimated time for the robot to reach the specified 6DOF pose.
    /// </summary>
    /// <param name="session">The connected OPC UA session.</param>
    /// <param name="x">Target X position in millimeters.</param>
    /// <param name="y">Target Y position in millimeters.</param>
    /// <param name="z">Target Z position in millimeters.</param>
    /// <param name="alpha">Target alpha rotation in degrees.</param>
    /// <param name="beta">Target beta rotation in degrees.</param>
    /// <param name="gamma">Target gamma rotation in degrees.</param>
    /// <returns>Estimated execution time in seconds, or null if the call fails.</returns>
    public double? CallGetExecutionEstimateMethod(
        ISession session,
        double x, double y, double z,
        double alpha, double beta, double gamma)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!session.Connected)
        {
            _logger.LogCannotCallMethodNotConnected();
            return null;
        }

        try
        {
            // First find the robot node by browsing Objects
            var robotNode = FindRobotNode(session);
            if (robotNode == null)
            {
                _logger.LogDataReaderRobotNodeNotFound(RobotNodePath);
                return null;
            }

            // Method needs to be found by browsing the robot node's methods
            var methodNode = FindMethodNode(session, robotNode, "GetExecutionEstimateAsync");
            if (methodNode == null)
            {
                _logger.LogGetExecutionEstimateMethodNotFound();
                return null;
            }

            _logger.LogCallingOpcUaMethod(robotNode, methodNode);
            _logger.LogMethodParameters(x, y, z, alpha, beta, gamma);

            var inputArguments = new object[] { x, y, z, alpha, beta, gamma };
            var outputArguments = session.Call(robotNode, methodNode, inputArguments);

            if (outputArguments == null || outputArguments.Count == 0)
            {
                _logger.LogMethodCallNoOutput();
                return null;
            }

            var estimatedDuration = Convert.ToDouble(outputArguments[0], CultureInfo.InvariantCulture);
            _logger.LogDataReaderExecutionEstimateReceived(estimatedDuration);

            return estimatedDuration;
        }
        catch (Exception ex)
        {
            _logger.LogGetExecutionEstimateError(ex);
            return null;
        }
    }

    /// <summary>
    ///     Finds the robot node by browsing the Objects folder.
    /// </summary>
    /// <param name="session">The connected OPC UA session.</param>
    /// <returns>The NodeId of the robot node, or null if not found.</returns>
    private NodeId? FindRobotNode(ISession session)
    {
        try
        {
            _logger.LogDataReaderBrowsingForRobotNode(RobotNodePath);

            // Browse the Objects node
            var nodeToBrowse = new BrowseDescription
            {
                NodeId = ObjectIds.ObjectsFolder,
                BrowseDirection = BrowseDirection.Forward,
                ReferenceTypeId = ReferenceTypeIds.Organizes,
                IncludeSubtypes = true,
                NodeClassMask = (uint)NodeClass.Object,
                ResultMask = (uint)BrowseResultMask.All
            };

            var nodesToBrowse = new BrowseDescriptionCollection { nodeToBrowse };
            session.Browse(null, null, 0, nodesToBrowse, out var results, out _);

            if (results == null || results.Count == 0)
            {
                _logger.LogDataReaderBrowseNoResultsForObjectsFolder();
                return null;
            }

            var browseResult = results[0];
            if (StatusCode.IsBad(browseResult.StatusCode))
            {
                _logger.LogDataReaderBrowseFailedWithStatus(browseResult.StatusCode);
                return null;
            }

            // Search for the robot by browse name
            foreach (var nodeId in from reference in browseResult.References
                                   where reference.BrowseName.Name == RobotNodePath
                                   select ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris))
            {
                _logger.LogDataReaderFoundRobotNode(RobotNodePath, nodeId);
                return nodeId;
            }

            _logger.LogDataReaderRobotNodeNotFoundAmongChildren(RobotNodePath, browseResult.References.Count);

            // Log all found children for debugging
            if (browseResult.References.Count > 0 && _logger.IsEnabled(LogLevel.Trace))
                _logger.LogAvailableObjects(
                    string.Join(", ", browseResult.References.Select(r => r.BrowseName.Name)));

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDataReaderBrowsingRobotNodeError(ex, RobotNodePath);
            return null;
        }
    }

    /// <summary>
    ///     Finds a method node by browsing the children of a parent node.
    /// </summary>
    /// <param name="session">The connected OPC UA session.</param>
    /// <param name="parentNodeId">The parent node to browse.</param>
    /// <param name="methodName">The name of the method to find.</param>
    /// <returns>The NodeId of the method, or null if not found.</returns>
    private NodeId? FindMethodNode(ISession session, NodeId parentNodeId, string methodName)
    {
        try
        {
            _logger.LogDataReaderBrowsingForMethod(parentNodeId, methodName);

            // Browse the parent node to find all children
            var nodeToBrowse = new BrowseDescription
            {
                NodeId = parentNodeId,
                BrowseDirection = BrowseDirection.Forward,
                ReferenceTypeId = ReferenceTypeIds.HasComponent,
                IncludeSubtypes = true,
                NodeClassMask = (uint)NodeClass.Method,
                ResultMask = (uint)BrowseResultMask.All
            };

            var nodesToBrowse = new BrowseDescriptionCollection { nodeToBrowse };
            session.Browse(null, null, 0, nodesToBrowse, out var results, out _);

            if (results == null || results.Count == 0)
            {
                _logger.LogDataReaderBrowseNoResultsForNode(parentNodeId);
                return null;
            }

            var browseResult = results[0];
            if (StatusCode.IsBad(browseResult.StatusCode))
            {
                _logger.LogDataReaderBrowseFailedWithStatus(browseResult.StatusCode);
                return null;
            }

            // Search for the method by browse name
            foreach (var reference in
                     browseResult.References.Where(reference => reference.BrowseName.Name == methodName))
            {
                _logger.LogDataReaderFoundMethod(methodName, reference.NodeId);
                return ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris);
            }

            _logger.LogDataReaderMethodNotFoundAmongChildren(methodName, browseResult.References.Count, parentNodeId);

            // Log all found children for debugging
            if (browseResult.References.Count > 0 && _logger.IsEnabled(LogLevel.Trace))
                _logger.LogAvailableChildren(
                    string.Join(", ", browseResult.References.Select(r => r.BrowseName.Name)));

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDataReaderBrowsingMethodError(ex, methodName);
            return null;
        }
    }
}