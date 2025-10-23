"""
Visualization functions for PCM-LLM simulation trajectories.

This module provides plotting utilities for:
- Time series trajectory plots with predictions
- Spatial position visualization on 2D grids
- Statistical analysis plots (mean curves, confidence intervals)
- Multi-condition comparison plots
- Bar charts and progress bars for experimental results

All plots are designed for analyzing treasure-hunting game simulations
with theory of mind, preference modeling, and emotion prediction.
"""

import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import matplotlib.cm as cm
import pprint
pp = pprint.PrettyPrinter()
from matplotlib.lines import Line2D
from functions import *
import seaborn as sns
from matplotlib import gridspec
from matplotlib.patches import Patch
from matplotlib.patches import Rectangle

# Box object mapping for treasure-hunting game
object_dict = {0: "dark_brown_box", 1: "light_brown_box"}

def bar_plot(conditions, categories, X_values, p_values, title):
    """
    Create horizontal bar plot with statistical significance markers.

    Displays chi-square test results with significance stars (*, **, ***).

    Args:
        conditions: List of condition names (e.g., ["ToM=0", "ToM=1"])
        categories: List of category names for y-axis labels
        X_values: Dict {category: [chi2_values_per_condition]}
        p_values: Dict {category: [p_values_per_condition]}
        title: Filename for saved figure

    Outputs:
        Saves figure to Images/{title}
    """
    colors = ["#A2D94D", "#72D3D4", "#859ED7", "#8F69C5"]

    def get_stars(p):
        """Convert p-value to significance stars."""
        if p < 0.001:
            return '***'
        elif p < 0.01:
            return '**'
        elif p < 0.05:
            return '*'
        else:
            return ''

    # Create plot
    fig, ax = plt.subplots(figsize=(10, 6), dpi=300)
    bar_height = 1/len(conditions) * 0.8
    y = np.arange(len(categories))

    for i, condition in enumerate(conditions):
        offset = (i - 1) * bar_height
        for j, category in enumerate(categories):
            width = X_values[category][i]
            p_val = p_values[category][i]
            print(condition, category, width, p_val)
            ax.barh(y[j] + offset, width, height=bar_height, color=colors[i], edgecolor="black", linewidth=1, label=condition if j == 0 else "")
            ax.text(width + 0.01, y[j] + offset, get_stars(p_val),
                    va='center', ha='left', fontsize=18)

    # Aesthetics
    ax.set_yticks(y)
    ax.set_yticklabels(categories, fontsize=18)  
    ax.set_xlabel('$\chi^2$', fontsize=18)             
    #ax.set_title('Horizontal bar plot of significant results by condition', fontsize=20)  
    ax.tick_params(labelsize=18)                 
    ax.legend(fontsize=18)                       
    plt.tight_layout()
    plt.savefig(f"Images/{title}", dpi=300)
    plt.show()



def draw_progress_bars(success_ratios, fail_ratios, width=5, height=1):
    """
    Draw horizontal progress bars showing success/failure proportions.

    Args:
        success_ratios: List of success rates (0 to 1) for each bar
        fail_ratios: List of failure rates (0 to 1) for each bar
        width: Bar width (default: 5)
        height: Bar height (default: 1)

    Raises:
        ValueError: If list lengths don't match
    """
    if len(success_ratios) != len(fail_ratios):
        raise ValueError("Success and failure ratio lists must have the same length")
    
    num_bars = len(success_ratios)
    fig, axes = plt.subplots(num_bars, 1, figsize=(width, height * num_bars * 1.2), 
                            gridspec_kw={'hspace': 0.8}) 
    
    if num_bars == 1:
        axes = [axes]
    
    for i, (ax, s_ratio, f_ratio) in enumerate(zip(axes, success_ratios, fail_ratios)):
        total_ratio = s_ratio + f_ratio
        remaining_ratio = 1.0 - total_ratio if total_ratio <= 1.0 else 0
        
        border = Rectangle((0, 0), width, height, 
                                  edgecolor='black', facecolor='none', lw=1.5)
        ax.add_patch(border)
        
        success_rect = Rectangle((0, 0), width * s_ratio, height,
                                       facecolor='#E74C3C', alpha=0.9)  # Red
        ax.add_patch(success_rect)

        fail_rect = Rectangle((width * s_ratio, 0), width * f_ratio, height,
                                    facecolor='#3498DB', alpha=0.9)  # Blue
        ax.add_patch(fail_rect)
        
        if s_ratio > 0:
            success_text = f"{s_ratio:.0%}" if s_ratio >= 0.1 else f"{s_ratio:.1%}"
            ax.text(width * s_ratio / 2, height/2, success_text,
                    ha='center', va='center', color='white', fontsize=10, fontweight='bold')
        
    
        if f_ratio > 0:
            fail_text = f"{f_ratio:.0%}" if f_ratio >= 0.1 else f"{f_ratio:.1%}"
            ax.text(width * s_ratio + width * f_ratio / 2, height/2, fail_text,
                    ha='center', va='center', color='white', fontsize=10, fontweight='bold')
        

        # Only show remaining portion if > 5%
        if remaining_ratio > 0.05:
            ax.text(width * (s_ratio + f_ratio) + width * remaining_ratio / 2, height/2,
                    f"{remaining_ratio:.0%}",
                    ha='center', va='center', color='black', fontsize=9)
        

        ax.text(-0.02, height/2, f"Bar {i+1}", 
                ha='right', va='center', fontsize=9)
        
  
        ax.set_xlim(-0.3, width) 
        ax.set_ylim(0, height)
        ax.set_aspect('equal')
        ax.set_xticks([])
        ax.set_yticks([])
        ax.set_frame_on(False)
    

    fig.legend(handles=[
        Patch(facecolor='#E74C3C', label='Success'),
        Patch(facecolor='#3498DB', label='Failure'),
        Patch(facecolor='white', label='Remaining')
    ], loc='upper right', ncol=3, frameon=False)
    
    plt.tight_layout()
    plt.subplots_adjust(top=0.9)  # Leave space for legend
    plt.show()
    
    
def trajectories_mean_curve_subplot(agent_role, verbal_mode, category, agent, variable, entity=None, score=None):
    """
    Plot mean trajectory curves with confidence intervals across simulations.

    Creates subplot grid showing mean/median curves with IQR and 95% CI
    for each experimental condition combination (ToM, facial expression, etc.).

    Args:
        agent_role: Agent role filter (0=adversary, 1=partner)
        verbal_mode: Verbal mode filter (e.g., "Verbal", "NonVerbal")
        category: Data category (e.g., "real_agent_data")
        agent: Agent name (e.g., "subject", "participant")
        variable: Variable to plot (e.g., "preference", "valence")
        entity: Entity name or None (uses "correct box" if None)
        score: Optional agent score filter

    Outputs:
        Saves to Images/evolution_{agent_role}_{verbal_mode}_{category}_{variable}_{agent}_{entity}_{score}.png
    """
    if entity == None:
        df = pd.DataFrame()
        for nb, entity in object_dict.items():
            # Load the uploaded CSV file
            file_path = f"./simulation_results/{category}_{variable}_{agent}_{entity}.csv"
            
            df_data = pd.read_csv(file_path)
            if score == None: 
                df_data = df_data[(df_data["VirtualAgentRole"]==agent_role) & (df_data["VerbalMode"]==verbal_mode) & (df_data["RewardBoxID"]==nb) & (df_data["SelectedBox"] != -2)]
            else:
                df_data = df_data[(df_data["VirtualAgentRole"]==agent_role) & (df_data["VerbalMode"]==verbal_mode) & (df_data["RewardBoxID"]==nb) & (df_data["SelectedBox"] != -2) & (df_data["AgentScore"] == score)]
            df = pd.concat([df, df_data])
        entity = "correct box"
    else:
        # Load the uploaded CSV file
        file_path = f"./simulation_results/{category}_{variable}_{agent}_{entity}.csv"
        df = pd.read_csv(file_path)
        df = df[(df["VirtualAgentRole"]==agent_role) & (df["VerbalMode"]==verbal_mode) & (df["SelectedBox"] != -2) & (df["AgentScore"] == score)]
    
    # Melt the last 11 columns into long format for time-series plotting
    value_vars = [str(i) for i in range(11)]  # '0' to '10'
    df_long = df.melt(
        id_vars=["ToM", "FacialExpression", "ParticipantPhysiologicalSensitivity"],
        value_vars=value_vars,
        var_name="TimeStep",
        value_name="Value"
    )

    # Convert TimeStep to integer for plotting
    df_long["TimeStep"] = df_long["TimeStep"].astype(int)

    # Create a grouped label column
    df_long["Group"] = (
        "ToM=" + df_long["ToM"].astype(str) +
        ", VCFE=" + df_long["FacialExpression"].astype(str) +
        ", w_ps=" + df_long["ParticipantPhysiologicalSensitivity"].astype(str)
    )

    # Plot spaghetti plots per condition group with mean line
    unique_groups = df_long["Group"].unique()
    num_groups = len(unique_groups)
    cols = 4
    rows = (num_groups + cols - 1) // cols

    fig = plt.figure(figsize=(25, rows * 4))
    gs = gridspec.GridSpec(rows, cols, hspace=0.4)
    
    cmap = ["black", "#912C2C", "#FCBB44", "#7A70B5", "#D3D3D3", "#FCBB44", "#F1766D", "#839DD1"]

    legend_elements = [
        # Mean line
        Line2D([0], [0], color=cmap[0], linewidth=2, label='Mean'),
        # Median line
        Line2D([0], [0], color=cmap[3], linestyle='--', linewidth=2, label='Median'),
        # Interquartile range (IQR)
        Patch(facecolor='#ecf0f1', edgecolor=cmap[2], alpha=0.4, label='IQR'),
        # 95% confidence interval
        Patch(facecolor='#bdc3c7', edgecolor=cmap[4], alpha=0.4, label='95% CI'),
    ]
    # Plot each group
    for idx, group in enumerate(sorted(unique_groups)):
        ax = fig.add_subplot(gs[idx])
        data_subset = df_long[df_long["Group"] == group]
        
        # Plot group median curve
        median_curve = data_subset.groupby("TimeStep")["Value"].median()
        ax.plot(median_curve.index, median_curve.values, color=cmap[3], linewidth=2, linestyle="--", label="Median")
        
        # Plot group mean
        mean_curve = data_subset.groupby("TimeStep")["Value"].mean()
        ax.plot(mean_curve.index, mean_curve.values, color=cmap[0], linewidth=2, label="Mean")
        
        # C. Statistical reference regions
        # Plot interquartile range (IQR)
        q1 = data_subset.groupby("TimeStep")["Value"].quantile(0.25)
        q3 = data_subset.groupby("TimeStep")["Value"].quantile(0.75)
        ax.fill_between(q1.index, q1.values, q3.values, color=cmap[2], alpha=0.2)
        
        # Plot 95% confidence interval
        if len(data_subset) > 1:
            lower_bound = data_subset.groupby("TimeStep")["Value"].quantile(0.025)
            upper_bound = data_subset.groupby("TimeStep")["Value"].quantile(0.975)
            ax.fill_between(mean_curve.index, lower_bound, upper_bound, 
                           color=cmap[4], alpha=0.4, label="95% CI")
        ax.grid()
        ax.set_title(group, fontsize=18)
        ax.set_xlabel("Time Step", fontsize=16)
        ax.set_ylabel("Value", fontsize=16)
        ax.set_ylim(df_long["Value"].min() - 0.05, df_long["Value"].max() + 0.05)

    # Add unified legend before displaying the plot
    plt.figlegend(handles=legend_elements, 
                loc='lower center', 
                ncol=4, 
                frameon=True,
                framealpha=0.8,
                facecolor='white',
                edgecolor='#34495e',
                bbox_to_anchor=(0.5, -0.05),  # Position below plots
                fontsize=18,
                title='Legend',
                title_fontsize=18)

    # Adjust layout to accommodate legend
    plt.tight_layout(rect=[0, 0.05, 1, 0.96])  # Leave space at bottom for legend
    plt.subplots_adjust(bottom=0.15)  # Increase bottom margin
    
    title = entity.replace("_", " ")
    category1 = category.split("_")[1]
    plt.suptitle(f"Average temporal evolution of {agent}'s {variable} towards {title} in {category1}'s belief across simulations", fontsize=16)
    plt.tight_layout(rect=[0, 0, 1, 0.96])
    plt.savefig(f"Images/evolution_{agent_role}_{verbal_mode}_{category}_{variable}_{agent}_{entity}_{score}.png", dpi=fig.dpi, bbox_inches='tight')
    plt.show()
    

def trajectories_mean_curve_distance_subplot(agent_role, verbal_mode, category, agent, variable, entity, score=None):
    """
    Plot mean curves of distance between agent and correct box.

    Computes absolute distance between agent position/state and the correct
    box position/state, then plots mean/median trajectories with confidence
    intervals across experimental conditions.

    Args:
        agent_role: Agent role filter (0=adversary, 1=partner)
        verbal_mode: Verbal mode filter
        category: Data category
        agent: Agent name
        variable: Variable name (usually position-related)
        entity: Entity name
        score: Optional agent score filter

    Outputs:
        Saves to Images/evolution_{agent_role}_{verbal_mode}_{category}_{variable}_{agent}_{entity}_{score}.png
    """
    # Load the CSV file
    file_path = f"./simulation_results/{category}_{variable}_{agent}_{entity}.csv"
    df_agent = pd.read_csv(file_path)
    
    if score == None: 
        df_agent = df_agent[(df_agent["VirtualAgentRole"]==agent_role) & (df_agent["VerbalMode"]==verbal_mode) & (df_agent['SelectedBox'] != -2)]
    else:
        df_agent = df_agent[(df_agent["VirtualAgentRole"]==agent_role) & (df_agent["VerbalMode"]==verbal_mode) & (df_agent['SelectedBox'] != -2) & (df_agent["AgentScore"] == score)]

    df_box = pd.DataFrame()
    for nb, box in object_dict.items():
        file_path = f"./simulation_results/{category}_{variable}_{box}_{entity}.csv"
        df_data = pd.read_csv(file_path)
        if score == None:
            df_data = df_data[(df_data["VirtualAgentRole"]==agent_role) & (df_data["VerbalMode"]==verbal_mode) & (df_data['SelectedBox'] != -2) & (df_data["RewardBoxID"]==nb)]
        else:
            df_data = df_data[(df_data["VirtualAgentRole"]==agent_role) & (df_data["VerbalMode"]==verbal_mode) & (df_data['SelectedBox'] != -2) & (df_data["RewardBoxID"]==nb) & (df_data["AgentScore"] == score)]
        df_box = pd.concat([df_box, df_data])
    
    value_columns = [str(i) for i in range(11)]  # Column names: ["0", "1", ..., "10"]

    # Rename columns to avoid conflicts
    box_renamed = df_box.rename(columns={col: f'box_{col}' for col in value_columns})
    box_renamed = box_renamed[["TimeStamp", "SimulationID", "box_0", "box_1", "box_2", "box_3", "box_4", "box_5", "box_6", "box_7", "box_8", "box_9", "box_10"]]

    # Merge dataframes
    merged_df = pd.merge(
        df_agent,
        box_renamed,
        on=["TimeStamp", "SimulationID"],
        how='inner'
    )

    # Calculate absolute distance
    for col in value_columns:
        merged_df[col] = np.abs(merged_df[col] - merged_df[f'box_{col}'])

    non_value_columns = [c for c in df_agent.columns 
                        if c not in value_columns and c not in ["TimeStamp", "SimulationID"]]

    result_df = merged_df[["TimeStamp", "SimulationID"] + non_value_columns + value_columns]

    # Melt the last 11 columns into long format for time-series plotting
    value_vars = [str(i) for i in range(11)]  # '0' to '10'
    df_long = result_df.melt(
        id_vars=["TimeStamp", "SimulationID", "ToM", "FacialExpression", "ParticipantPhysiologicalSensitivity", "RewardBoxID"],
        value_vars=value_vars,
        var_name="TimeStep",
        value_name="Value"
    )

    # Convert TimeStep to integer for plotting
    df_long["TimeStep"] = df_long["TimeStep"].astype(int)

    # Create a grouped label column
    df_long["Group"] = (
        "ToM=" + df_long["ToM"].astype(str) +
        ", VCFE=" + df_long["FacialExpression"].astype(str) +
        ", w_ps=" + df_long["ParticipantPhysiologicalSensitivity"].astype(str)
    )
    
    df_long["SimulationID"] = df_long["SimulationID"].astype(str)
    df_long["TimeStamp"] = df_long["TimeStamp"].astype(str)
    df_long["UniqueSimID"] = df_long["TimeStamp"] + "_" + df_long["SimulationID"]
    df_long = df_long.sort_values(['UniqueSimID', 'TimeStep'])


    # Plot spaghetti plots per condition group with mean line
    unique_groups = df_long["Group"].unique()
    num_groups = len(unique_groups)
    cols = 4
    rows = (num_groups + cols - 1) // cols

    fig = plt.figure(figsize=(25, rows * 4))
    gs = gridspec.GridSpec(rows, cols, hspace=0.4)
    
    cmap = ["black", "#2E2585", "#FCBB44", "#7A70B5", "#D3D3D3", "#FCBB44", "#F1766D", "#839DD1"]

    legend_elements = [
        # Mean line
        Line2D([0], [0], color=cmap[0], linewidth=2, label='Mean'),
        # Median line
        Line2D([0], [0], color=cmap[3], linestyle='--', linewidth=2, label='Median'),
        # Interquartile range (IQR)
        Patch(facecolor='#ecf0f1', edgecolor=cmap[2], alpha=0.4, label='IQR'),
        # 95% confidence interval
        Patch(facecolor='#bdc3c7', edgecolor=cmap[4], alpha=0.4, label='95% CI'),
    ]
    # Plot each group
    for idx, group in enumerate(sorted(unique_groups)):
        ax = fig.add_subplot(gs[idx])
        data_subset = df_long[df_long["Group"] == group]
        
        # Plot group median curve
        median_curve = data_subset.groupby("TimeStep")["Value"].median()
        ax.plot(median_curve.index, median_curve.values, color=cmap[3], linewidth=2, linestyle="--", label="Median")
        
        # Plot group mean
        mean_curve = data_subset.groupby("TimeStep")["Value"].mean()
        ax.plot(mean_curve.index, mean_curve.values, color=cmap[0], linewidth=2, label="Mean")
        
        # C. Statistical reference regions
        # Plot interquartile range (IQR)
        q1 = data_subset.groupby("TimeStep")["Value"].quantile(0.25)
        q3 = data_subset.groupby("TimeStep")["Value"].quantile(0.75)
        ax.fill_between(q1.index, q1.values, q3.values, color=cmap[2], alpha=0.2)
        
        # Plot 95% confidence interval
        if len(data_subset) > 1:
            lower_bound = data_subset.groupby("TimeStep")["Value"].quantile(0.025)
            upper_bound = data_subset.groupby("TimeStep")["Value"].quantile(0.975)
            ax.fill_between(mean_curve.index, lower_bound, upper_bound, 
                           color=cmap[4], alpha=0.4, label="95% CI")
        ax.grid()
        ax.set_title(group, fontsize=18)
        ax.set_xlabel("Time Step", fontsize=16)
        ax.set_ylabel("Value", fontsize=16)
        ax.set_ylim(df_long["Value"].min() - 0.05, df_long["Value"].max() + 0.05)

    # Add unified legend before displaying the plot
    plt.figlegend(handles=legend_elements, 
                loc='lower center', 
                ncol=4, 
                frameon=True,
                framealpha=0.8,
                facecolor='white',
                edgecolor='#34495e',
                bbox_to_anchor=(0.5, -0.05),  # Position below plots
                fontsize=18,
                title='Legend',
                title_fontsize=18)

    # Adjust layout to accommodate legend
    plt.tight_layout(rect=[0, 0.05, 1, 0.96])  # Leave space at bottom for legend
    plt.subplots_adjust(bottom=0.15)  # Increase bottom margin
    
    entity = entity.replace("_", " ")
    variable = variable.replace("_", " ")
    agent = agent.replace("_", " ")
    plt.suptitle(f"Average temporal evolution of distance between {agent} and correct box across simulations", fontsize=16)
    plt.tight_layout(rect=[0, 0, 1, 0.96])
    plt.savefig(f"Images/evolution_{agent_role}_{verbal_mode}_{category}_{variable}_{agent}_{entity}_{score}.png", dpi=fig.dpi, bbox_inches='tight')
    plt.show()
    

def verbal_trajectories_mean_curve_distance_subplot(agent_role, verbal_mode, category, agent, variable, entity, score):
    """
    Plot mean curves of distance between agent and correct box (verbal mode).

    Similar to trajectories_mean_curve_distance_subplot but for verbal mode
    simulations. Groups by ToM and LLMHypothesis instead of facial expression.

    Args:
        agent_role: Agent role filter (0=adversary, 1=partner)
        verbal_mode: Verbal mode filter
        category: Data category
        agent: Agent name
        variable: Variable name
        entity: Entity name
        score: Agent score filter (unused, present for consistency)

    Outputs:
        Saves to Images/evolution_{agent_role}_{verbal_mode}_{category}_{variable}_{agent}_{score}.png
    """
    # Load the uploaded CSV file
    file_path = f"./simulation_results/{category}_{variable}_{agent}_{entity}.csv"
    df_agent = pd.read_csv(file_path)
    df_agent = df_agent[(df_agent["VirtualAgentRole"]==agent_role) & (df_agent["VerbalMode"]==verbal_mode) & (df_agent['SelectedBox'] != -2)]

    df_box = pd.DataFrame()
    for nb, box in object_dict.items():
        file_path = f"./simulation_results/{category}_{variable}_{box}_{entity}.csv"
        df_data = pd.read_csv(file_path)
        df_data = df_data[(df_data["VirtualAgentRole"]==agent_role) & (df_data["VerbalMode"]==verbal_mode) & (df_data['SelectedBox'] != -2) & (df_data["RewardBoxID"]==nb)]
        df_box = pd.concat([df_box, df_data])
    
    value_columns = [str(i) for i in range(11)]  # Column names: ["0", "1", ..., "10"]

    # Rename columns to avoid conflicts
    box_renamed = df_box.rename(columns={col: f'box_{col}' for col in value_columns})
    box_renamed = box_renamed[["TimeStamp", "SimulationID", "box_0", "box_1", "box_2", "box_3", "box_4", "box_5", "box_6", "box_7", "box_8", "box_9", "box_10"]]

    # Merge dataframes
    merged_df = pd.merge(
        df_agent,
        box_renamed,
        on=["TimeStamp", "SimulationID"],
        how='inner'
    )

    # Calculate absolute distance
    for col in value_columns:
        merged_df[col] = np.abs(merged_df[col] - merged_df[f'box_{col}'])

    non_value_columns = [c for c in df_agent.columns 
                        if c not in value_columns and c not in ["TimeStamp", "SimulationID"]]

    result_df = merged_df[["TimeStamp", "SimulationID"] + non_value_columns + value_columns]

    # Melt the last 11 columns into long format for time-series plotting
    value_vars = [str(i) for i in range(11)]  # '0' to '10'
    df_long = result_df.melt(
        id_vars=["TimeStamp", "SimulationID", "ToM", "LLMHypothesis", "RewardBoxID"],
        value_vars=value_vars,
        var_name="TimeStep",
        value_name="Value"
    )

    # Convert TimeStep to integer for plotting
    df_long["TimeStep"] = df_long["TimeStep"].astype(int)

    # Create a grouped label column
    df_long["Group"] = (
        "C" + (df_long["LLMHypothesis"]+1).astype(str) +
        ", ToM=" + df_long["ToM"].astype(str) 
    )
    
    df_long["SimulationID"] = df_long["SimulationID"].astype(str)
    df_long["TimeStamp"] = df_long["TimeStamp"].astype(str)
    df_long["UniqueSimID"] = df_long["TimeStamp"] + "_" + df_long["SimulationID"]
    df_long = df_long.sort_values(['UniqueSimID', 'TimeStep'])


    # Plot spaghetti plots per condition group with mean line
    unique_groups = df_long["Group"].unique()
    num_groups = len(unique_groups)
    cols = 4
    rows = (num_groups + cols - 1) // cols

    fig = plt.figure(figsize=(25, rows * 4), dpi=300)
    gs = gridspec.GridSpec(rows, cols, hspace=0.4)
    
    cmap = ["black", "#2E2585", "#FCBB44", "#7A70B5", "#D3D3D3", "#FCBB44", "#F1766D", "#839DD1"]

    legend_elements = [
        # Mean line
        Line2D([0], [0], color=cmap[0], linewidth=2, label='Mean'),
        # Median line
        Line2D([0], [0], color=cmap[3], linestyle='--', linewidth=2, label='Median'),
        # Interquartile range (IQR)
        Patch(facecolor='#ecf0f1', edgecolor=cmap[2], alpha=0.4, label='IQR'),
        # 95% confidence interval
        Patch(facecolor='#bdc3c7', edgecolor=cmap[4], alpha=0.4, label='95% CI'),
    ]
    # Plot each group
    for idx, group in enumerate(sorted(unique_groups)):
        ax = fig.add_subplot(gs[idx])
        data_subset = df_long[df_long["Group"] == group]
        
        # Plot group median curve
        median_curve = data_subset.groupby("TimeStep")["Value"].median()
        ax.plot(median_curve.index, median_curve.values, color=cmap[3], linewidth=2, linestyle="--", label="Median")
        
        # Plot group mean
        mean_curve = data_subset.groupby("TimeStep")["Value"].mean()
        ax.plot(mean_curve.index, mean_curve.values, color=cmap[0], linewidth=2, label="Mean")
        
        # C. Statistical reference regions
        # Plot interquartile range (IQR)
        q1 = data_subset.groupby("TimeStep")["Value"].quantile(0.25)
        q3 = data_subset.groupby("TimeStep")["Value"].quantile(0.75)
        ax.fill_between(q1.index, q1.values, q3.values, color=cmap[2], alpha=0.2)
        
        # Plot 95% confidence interval
        if len(data_subset) > 1:
            lower_bound = data_subset.groupby("TimeStep")["Value"].quantile(0.025)
            upper_bound = data_subset.groupby("TimeStep")["Value"].quantile(0.975)
            ax.fill_between(mean_curve.index, lower_bound, upper_bound, 
                           color=cmap[4], alpha=0.4, label="95% CI")
        ax.grid()
        ax.set_title(group, fontsize=18)
        ax.set_xlabel("Time Step", fontsize=16)
        ax.set_ylabel("Value", fontsize=16)
        ax.set_ylim(df_long["Value"].min() - 0.05, df_long["Value"].max() + 0.05)

    
        # Add unified legend before displaying the plot
    plt.figlegend(handles=legend_elements, 
                loc='lower center', 
                ncol=4, 
                frameon=True,
                framealpha=0.8,
                facecolor='white',
                edgecolor='#34495e',
                bbox_to_anchor=(0.5, -0.2),  # Position below plots
                fontsize=18,
                title='Legend',
                title_fontsize=18)

    entity = entity.replace("_", " ")
    #plt.suptitle(f"Average temporal evolution of {agent} {entity} expression across simulations", fontsize=16)
    plt.subplots_adjust(top=0.8, bottom=0.2)  # Increase bottom margin
    plt.tight_layout(rect=[0, 0, 1, 0.96])
    plt.savefig(f"Images/evolution_{agent_role}_{verbal_mode}_{category}_{variable}_{agent}_{score}.png", dpi=fig.dpi, bbox_inches='tight')
    plt.show()
    
    
def verbal_trajectories_mean_curve_subplot(agent_role, verbal_mode, category, agent, variable, entity=None, score=None):
    """
    Plot mean curves for verbal mode simulations with LLM hypothesis grouping.

    Version 1: Does NOT filter by agent score. Groups by ToM and LLMHypothesis.

    Args:
        agent_role: Agent role filter (0=adversary, 1=partner)
        verbal_mode: Verbal mode filter
        category: Data category
        agent: Agent name
        variable: Variable to plot
        entity: Entity name or None (uses "correct box" if None)
        score: Unused in this version

    Outputs:
        Saves to Images/evolution_{agent_role}_{verbal_mode}_{category}_{variable}_{agent}_{score}.png

    Note:
        This is the first of two identically-named functions. This version does NOT
        filter by AgentScore. See line 714 for the score-filtering version.
    """
    if entity == None:
        df = pd.DataFrame()
        for nb, entity in object_dict.items():
            # Load the uploaded CSV file
            file_path = f"./simulation_results/{category}_{variable}_{agent}_{entity}.csv"

            df_data = pd.read_csv(file_path)
            df_data = df_data[(df_data["VirtualAgentRole"]==agent_role) & (df_data["VerbalMode"]==verbal_mode) & (df_data["RewardBoxID"]==nb) & (df_data["SelectedBox"] != -2)]
            df = pd.concat([df, df_data])
        entity = "correct box"
    else:
        # Load the uploaded CSV file
        file_path = f"./simulation_results/{category}_{variable}_{agent}_{entity}.csv"
        df = pd.read_csv(file_path)
        df = df[(df["VirtualAgentRole"]==agent_role) & (df["VerbalMode"]==verbal_mode) & (df["SelectedBox"] != -2)]
    
    # Melt the last 11 columns into long format for time-series plotting
    value_vars = [str(i) for i in range(11)]  # '0' to '10'
    df_long = df.melt(
        id_vars=["ToM", "LLMHypothesis"],
        value_vars=value_vars,
        var_name="TimeStep",
        value_name="Value"
    )

    # Convert TimeStep to integer for plotting
    df_long["TimeStep"] = df_long["TimeStep"].astype(int)

    # Create a grouped label column
    df_long["Group"] = (
        "C" + (df_long["LLMHypothesis"]+1).astype(str) +
        ", ToM=" + df_long["ToM"].astype(str) 
    )

    # Plot spaghetti plots per condition group with mean line
    unique_groups = df_long["Group"].unique()
    num_groups = len(unique_groups)
    cols = 4
    rows = (num_groups + cols - 1) // cols

    fig = plt.figure(figsize=(25, rows * 4))
    gs = gridspec.GridSpec(rows, cols, hspace=0.4)
    
    cmap = ["black", "#912C2C", "#FCBB44", "#7A70B5", "#D3D3D3", "#FCBB44", "#F1766D", "#839DD1"]

    legend_elements = [
        # Mean line
        Line2D([0], [0], color=cmap[0], linewidth=2, label='Mean'),
        # Median line
        Line2D([0], [0], color=cmap[3], linestyle='--', linewidth=2, label='Median'),
        # Interquartile range (IQR)
        Patch(facecolor='#ecf0f1', edgecolor=cmap[2], alpha=0.4, label='IQR'),
        # 95% confidence interval
        Patch(facecolor='#bdc3c7', edgecolor=cmap[4], alpha=0.4, label='95% CI'),
    ]
    # Plot each group
    for idx, group in enumerate(sorted(unique_groups)):
        ax = fig.add_subplot(gs[idx])
        data_subset = df_long[df_long["Group"] == group]
        
        # Plot group median curve
        median_curve = data_subset.groupby("TimeStep")["Value"].median()
        ax.plot(median_curve.index, median_curve.values, color=cmap[3], linewidth=2, linestyle="--", label="Median")
        
        # Plot group mean
        mean_curve = data_subset.groupby("TimeStep")["Value"].mean()
        ax.plot(mean_curve.index, mean_curve.values, color=cmap[0], linewidth=2, label="Mean")
        
        # C. Statistical reference regions
        # Plot interquartile range (IQR)
        q1 = data_subset.groupby("TimeStep")["Value"].quantile(0.25)
        q3 = data_subset.groupby("TimeStep")["Value"].quantile(0.75)
        ax.fill_between(q1.index, q1.values, q3.values, color=cmap[2], alpha=0.2)
        
        # Plot 95% confidence interval
        if len(data_subset) > 1:
            lower_bound = data_subset.groupby("TimeStep")["Value"].quantile(0.025)
            upper_bound = data_subset.groupby("TimeStep")["Value"].quantile(0.975)
            ax.fill_between(mean_curve.index, lower_bound, upper_bound, 
                           color=cmap[4], alpha=0.4, label="95% CI")
        ax.grid()
        ax.set_title(group, fontsize=14)
        ax.set_xlabel("Time Step", fontsize=14)
        ax.set_ylabel("Value", fontsize=14)
        ax.set_ylim(df_long["Value"].min() - 0.05, df_long["Value"].max() + 0.05)

    # Add unified legend before displaying the plot
    plt.figlegend(handles=legend_elements, 
                loc='lower center', 
                ncol=4, 
                frameon=True,
                framealpha=0.8,
                facecolor='white',
                edgecolor='#34495e',
                bbox_to_anchor=(0.5, -0.02),  # Position below plots
                fontsize=11,
                title='Legend',
                title_fontsize=12)

    # Adjust layout to accommodate legend
    plt.tight_layout(rect=[0, 0.05, 1, 0.96])  # Leave space at bottom for legend
    plt.subplots_adjust(bottom=0.15)  # Increase bottom margin
    
    entity = entity.replace("_", " ")
    plt.suptitle(f"Average temporal evolution of {agent} {entity} expression across simulations", fontsize=16)
    plt.tight_layout(rect=[0, 0, 1, 0.96])
    plt.savefig(f"Images/evolution_{agent_role}_{verbal_mode}_{category}_{variable}_{agent}_{score}.png", dpi=fig.dpi, bbox_inches='tight')
    plt.show()


def verbal_trajectories_mean_curve_subplot(agent_role, verbal_mode, category, agent, variable, entity=None, score=None):
    """
    Plot mean curves for verbal mode simulations with LLM hypothesis grouping.

    Version 2: FILTERS by agent score. Groups by ToM and LLMHypothesis.

    Args:
        agent_role: Agent role filter (0=adversary, 1=partner)
        verbal_mode: Verbal mode filter
        category: Data category
        agent: Agent name
        variable: Variable to plot
        entity: Entity name or None (uses "correct box" if None)
        score: Agent score filter (REQUIRED - filters data by AgentScore)

    Outputs:
        Saves to Images/evolution_{agent_role}_{verbal_mode}_{category}_{variable}_{agent}_{entity}_{score}.png

    Note:
        This is the second of two identically-named functions. This version FILTERS
        by AgentScore (line 722, 729). See line 468 for the non-filtering version.
    """
    if entity == None:
        df = pd.DataFrame()
        for nb, entity in object_dict.items():
            # Load the uploaded CSV file
            file_path = f"./simulation_results/{category}_{variable}_{agent}_{entity}.csv"

            df_data = pd.read_csv(file_path)
            df_data = df_data[(df_data["VirtualAgentRole"]==agent_role) & (df_data["VerbalMode"]==verbal_mode) & (df_data["RewardBoxID"]==nb) & (df_data["SelectedBox"] != -2) & (df_data["AgentScore"] == score)]
            df = pd.concat([df, df_data])
        entity = "correct box"
    else:
        # Load the uploaded CSV file
        file_path = f"./simulation_results/{category}_{variable}_{agent}_{entity}.csv"
        df = pd.read_csv(file_path)
        df = df[(df["VirtualAgentRole"]==agent_role) & (df["VerbalMode"]==verbal_mode) & (df["SelectedBox"] != -2) & (df["AgentScore"] == score)]
    
    # Melt the last 11 columns into long format for time-series plotting
    value_vars = [str(i) for i in range(11)]  # '0' to '10'
    df_long = df.melt(
        id_vars=["ToM", "LLMHypothesis"],
        value_vars=value_vars,
        var_name="TimeStep",
        value_name="Value"
    )

    # Convert TimeStep to integer for plotting
    df_long["TimeStep"] = df_long["TimeStep"].astype(int)

    # Create a grouped label column
    df_long["Group"] = (
        "C" + (df_long["LLMHypothesis"]+1).astype(str) +
        ", ToM=" + df_long["ToM"].astype(str) 
    )

    # Plot spaghetti plots per condition group with mean line
    unique_groups = df_long["Group"].unique()
    num_groups = len(unique_groups)
    cols = 4
    rows = (num_groups + cols - 1) // cols

    fig = plt.figure(figsize=(25, rows * 4), dpi=300)
    gs = gridspec.GridSpec(rows, cols, hspace=0.4)
    
    cmap = ["black", "#912C2C", "#FCBB44", "#7A70B5", "#D3D3D3", "#FCBB44", "#F1766D", "#839DD1"]

    legend_elements = [
        # Mean line
        Line2D([0], [0], color=cmap[0], linewidth=2, label='Mean'),
        # Median line
        Line2D([0], [0], color=cmap[3], linestyle='--', linewidth=2, label='Median'),
        # Interquartile range (IQR)
        Patch(facecolor='#ecf0f1', edgecolor=cmap[2], alpha=0.4, label='IQR'),
        # 95% confidence interval
        Patch(facecolor='#bdc3c7', edgecolor=cmap[4], alpha=0.4, label='95% CI')

    ]
    # Plot each group
    for idx, group in enumerate(sorted(unique_groups)):
        ax = fig.add_subplot(gs[idx])
        data_subset = df_long[df_long["Group"] == group]
        
        # Plot group median curve
        median_curve = data_subset.groupby("TimeStep")["Value"].median()
        ax.plot(median_curve.index, median_curve.values, color=cmap[3], linewidth=2, linestyle="--", label="Median"),
        
        # Plot group mean
        mean_curve = data_subset.groupby("TimeStep")["Value"].mean()
        ax.plot(mean_curve.index, mean_curve.values, color=cmap[0], linewidth=2, label="Mean"),
        
        # C. Statistical reference regions
        # Plot interquartile range (IQR)
        q1 = data_subset.groupby("TimeStep")["Value"].quantile(0.25)
        q3 = data_subset.groupby("TimeStep")["Value"].quantile(0.75)
        ax.fill_between(q1.index, q1.values, q3.values, color=cmap[2], alpha=0.2)
        
        # Plot 95% confidence interval
        if len(data_subset) > 1:
            lower_bound = data_subset.groupby("TimeStep")["Value"].quantile(0.025)
            upper_bound = data_subset.groupby("TimeStep")["Value"].quantile(0.975)
            ax.fill_between(mean_curve.index, lower_bound, upper_bound, 
                           color=cmap[4], alpha=0.4, label="95% CI")
        ax.grid()
        ax.set_title(group, fontsize=18)
        ax.set_xlabel("Time Step", fontsize=16)
        ax.set_ylabel("Value", fontsize=16)
        ax.set_ylim(df_long["Value"].min() - 0.05, df_long["Value"].max() + 0.05)

    # Add unified legend before displaying the plot
    plt.figlegend(handles=legend_elements, 
                loc='lower center', 
                ncol=4, 
                frameon=True,
                framealpha=0.8,
                facecolor='white',
                edgecolor='#34495e',
                bbox_to_anchor=(0.5, -0.2),  # Position below plots
                fontsize=18,
                title='Legend',
                title_fontsize=18)

    entity = entity.replace("_", " ")
    subject = category.split("_")[1]
    #plt.suptitle(f"Average temporal evolution of {variable} {agent} {entity} in {subject}'s belief across simulations", fontsize=16)
    plt.subplots_adjust(top=0.8, bottom=0.2)  # Increase bottom margin
    plt.tight_layout(rect=[0, 0, 1, 0.96])
    plt.savefig(f"Images/evolution_{agent_role}_{verbal_mode}_{category}_{variable}_{agent}_{entity}_{score}.png", dpi=fig.dpi, bbox_inches='tight')
    plt.show()